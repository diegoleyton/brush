using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.RemoteCommunication;
using Game.Core.Configuration;
using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Backend client for child profile operations.
    /// </summary>
    public sealed class ChildrenApiClient : IChildrenApiClient
    {
        private readonly BackendSettings settings_;
        private readonly IAuthService authService_;
        private readonly IRemoteRequestDispatcher remoteRequestDispatcher_;
        private readonly IRemotePayloadCodec payloadCodec_;
        private readonly IGameLogger logger_;

        public ChildrenApiClient(
            BackendSettings settings,
            IAuthService authService,
            IRemoteRequestDispatcher remoteRequestDispatcher,
            IRemotePayloadCodec payloadCodec,
            IGameLogger logger)
        {
            settings_ = settings ?? throw new ArgumentNullException(nameof(settings));
            authService_ = authService ?? throw new ArgumentNullException(nameof(authService));
            remoteRequestDispatcher_ = remoteRequestDispatcher ?? throw new ArgumentNullException(nameof(remoteRequestDispatcher));
            payloadCodec_ = payloadCodec ?? throw new ArgumentNullException(nameof(payloadCodec));
            logger_ = logger;
        }

        public async Task<IReadOnlyList<Profile>> ListAsync()
        {
            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children",
                    RemoteRequestMethod.Get,
                    isIdempotent: true));

            if (!response.IsSuccess)
            {
                throw new RemoteRequestFailedException(
                    ResolveRemoteErrorMessage(response, "Could not load profiles."),
                    response.IsNetworkError,
                    response.StatusCode);
            }

            ChildProfileListEnvelope envelope = payloadCodec_.Deserialize<ChildProfileListEnvelope>(response.Body);
            if (envelope?.children == null)
            {
                return Array.Empty<Profile>();
            }

            List<Profile> profiles = new List<Profile>(envelope.children.Length);
            for (int index = 0; index < envelope.children.Length; index++)
            {
                ChildProfileDto dto = envelope.children[index];
                if (dto == null)
                {
                    continue;
                }

                profiles.Add(ToProfile(dto));
            }

            return profiles;
        }

        public async Task<Profile> CreateAsync(string name, string petName, int pictureId)
        {
            string body = "{\"name\":\"" + EscapeJson(name) +
                          "\",\"petName\":\"" + EscapeJson(petName) +
                          "\",\"pictureId\":" + pictureId + "}";

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children",
                    RemoteRequestMethod.Post,
                    body: body,
                    isIdempotent: false));

            if (!response.IsSuccess)
            {
                return null;
            }

            ChildProfileDto dto = payloadCodec_.Deserialize<ChildProfileDto>(response.Body);
            return dto != null ? ToProfile(dto) : null;
        }

        public async Task<bool> DeleteAsync(string remoteProfileId)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId))
            {
                return false;
            }

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children/{remoteProfileId}",
                    RemoteRequestMethod.Delete,
                    isIdempotent: false));

            return response.IsSuccess && response.StatusCode == 204;
        }

        public async Task<bool> UpdateProfileAsync(string remoteProfileId, string name, string petName, int pictureId, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId))
            {
                return false;
            }

            string body = payloadCodec_.Serialize(new UpdateChildProfileRequest
            {
                Name = name ?? string.Empty,
                PetName = petName ?? string.Empty,
                PictureId = pictureId,
                IsActive = isActive
            });

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children/{remoteProfileId}",
                    RemoteRequestMethod.Patch,
                    body: body,
                    isIdempotent: false));

            return response.IsSuccess;
        }

        public async Task<ChildGameStateSnapshot> GetGameStateAsync(string remoteProfileId)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId))
            {
                return null;
            }

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children/{remoteProfileId}/game-state",
                    RemoteRequestMethod.Get,
                    isIdempotent: true));

            if (!response.IsSuccess)
            {
                return null;
            }

            return payloadCodec_.Deserialize<ChildGameStateSnapshot>(response.Body);
        }

        public async Task<ChildGameStateSyncPushResult> PushGameStateAsync(
            string remoteProfileId,
            string baseRevision,
            ChildGameStateSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId) || snapshot == null)
            {
                return new ChildGameStateSyncPushResult
                {
                    Status = ChildGameStateSyncPushStatus.ServerRejected,
                    ErrorMessage = "Missing child game state payload."
                };
            }

            string body = payloadCodec_.Serialize(new UpdateChildGameStateRequest
            {
                BaseRevision = baseRevision ?? string.Empty,
                BrushSessionDurationMinutes = snapshot.BrushSessionDurationMinutes,
                PendingReward = snapshot.PendingReward,
                Muted = snapshot.Muted,
                PetState = snapshot.PetState ?? new Pet(),
                RoomState = snapshot.RoomState ?? new Room(),
                InventoryState = snapshot.InventoryState ?? new Inventory()
            });

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children/{remoteProfileId}/game-state",
                    RemoteRequestMethod.Put,
                    body: body,
                    isIdempotent: false));

            if (response.IsSuccess)
            {
                return new ChildGameStateSyncPushResult
                {
                    Status = ChildGameStateSyncPushStatus.Success,
                    Snapshot = payloadCodec_.Deserialize<ChildGameStateSnapshot>(response.Body)
                };
            }

            string errorMessage = TryParseErrorMessage(response.Body);
            if (response.IsNetworkError)
            {
                return new ChildGameStateSyncPushResult
                {
                    Status = ChildGameStateSyncPushStatus.TransportFailure,
                    ErrorMessage = errorMessage
                };
            }

            if (response.StatusCode == 409)
            {
                return new ChildGameStateSyncPushResult
                {
                    Status = ChildGameStateSyncPushStatus.Conflict,
                    ErrorMessage = errorMessage
                };
            }

            return new ChildGameStateSyncPushResult
            {
                Status = ChildGameStateSyncPushStatus.ServerRejected,
                ErrorMessage = errorMessage
            };
        }

        public async Task<ChildGameStateSnapshot> CompleteBrushSessionAsync(string remoteProfileId)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId))
            {
                throw new RemoteRequestFailedException(
                    "Could not complete brush session.",
                    isNetworkError: false);
            }

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children/{remoteProfileId}/brush-completions",
                    RemoteRequestMethod.Post,
                    isIdempotent: false));

            if (!response.IsSuccess)
            {
                throw new RemoteRequestFailedException(
                    ResolveRemoteErrorMessage(response, "Could not complete brush session."),
                    response.IsNetworkError,
                    response.StatusCode);
            }

            return payloadCodec_.Deserialize<ChildGameStateSnapshot>(response.Body);
        }

        public async Task<Reward[]> ClaimRewardsAsync(string remoteProfileId)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId))
            {
                return null;
            }

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children/{remoteProfileId}/claim-rewards",
                    RemoteRequestMethod.Post,
                    isIdempotent: false));

            if (!response.IsSuccess)
            {
                return null;
            }

            RewardClaimResponseEnvelope envelope = payloadCodec_.Deserialize<RewardClaimResponseEnvelope>(response.Body);
            if (envelope?.rewards == null)
            {
                return null;
            }

            List<Reward> rewards = new List<Reward>(envelope.rewards.Length);
            for (int index = 0; index < envelope.rewards.Length; index++)
            {
                RewardClaimDto reward = envelope.rewards[index];
                if (reward == null)
                {
                    continue;
                }

                rewards.Add(new Reward
                {
                    Kind = (RewardKind)reward.kind,
                    RewardType = (InteractionPointType)reward.rewardType,
                    CurrencyType = (CurrencyType)reward.currencyType,
                    Id = reward.id,
                    Quantity = reward.quantity
                });
            }

            return rewards.ToArray();
        }

        public async Task<MarketPurchaseStatus> PurchaseMarketItemAsync(string remoteProfileId, MarketItemDefinition itemDefinition)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId) || itemDefinition == null)
            {
                return MarketPurchaseStatus.ITEM_NOT_FOUND;
            }

            string body = payloadCodec_.Serialize(new CreateInGameMarketPurchaseRequest
            {
                ItemType = (int)itemDefinition.ItemType,
                ItemId = itemDefinition.ItemId
            });

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children/{remoteProfileId}/market-purchases",
                    RemoteRequestMethod.Post,
                    body: body,
                    isIdempotent: false));

            if (response.IsSuccess)
            {
                return MarketPurchaseStatus.OK;
            }

            if (response.StatusCode == 404)
            {
                return MarketPurchaseStatus.ITEM_NOT_FOUND;
            }

            string errorMessage = TryParseErrorMessage(response.Body);
            if (errorMessage.Contains("already owned", StringComparison.OrdinalIgnoreCase))
            {
                return MarketPurchaseStatus.ALREADY_OWNED;
            }

            if (errorMessage.Contains("enough coins", StringComparison.OrdinalIgnoreCase))
            {
                return MarketPurchaseStatus.NOT_ENOUGH_CURRENCY;
            }

            if (response.StatusCode == 401)
            {
                return MarketPurchaseStatus.NO_CURRENT_PROFILE;
            }

            return MarketPurchaseStatus.ITEM_NOT_FOUND;
        }

        public async Task<bool> GrantCoinsAsync(string remoteProfileId, int amount, string reason)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId) || amount <= 0)
            {
                return false;
            }

            string body = "{\"amount\":" + amount +
                          ",\"reason\":\"" + EscapeJson(string.IsNullOrWhiteSpace(reason) ? "Debug grant" : reason) +
                          "\"}";

            RemoteResponse response = await SendAuthorizedAsync(
                new RemoteRequest(
                    $"{settings_.ApiBaseUrl}/children/{remoteProfileId}/grants",
                    RemoteRequestMethod.Post,
                    body: body,
                    isIdempotent: false));

            return response.IsSuccess;
        }

        private async Task<RemoteResponse> SendAuthorizedAsync(RemoteRequest request)
        {
            string token = authService_.CurrentSession?.AccessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                return new RemoteResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "No auth session is available."
                };
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> header in request.Headers)
            {
                headers[header.Key] = header.Value;
            }

            headers["Authorization"] = $"Bearer {token}";

            RemoteRequest authorizedRequest = new RemoteRequest(
                request.Url,
                request.Method,
                request.Body,
                headers,
                request.ContentType,
                request.TimeoutSeconds,
                request.IsIdempotent);

            RemoteResponse response = await remoteRequestDispatcher_.SendAsync(authorizedRequest);
            if (!response.IsSuccess)
            {
                string errorMessage = TryParseErrorMessage(response.Body);
                logger_?.Log($"[ChildrenApi] Remote request failed: {response.StatusCode} {errorMessage}");
            }

            return response;
        }

        private static Profile ToProfile(ChildProfileDto dto) =>
            new Profile
            {
                RemoteProfileId = dto.id,
                Name = dto.name,
                ProfilePictureId = dto.pictureId,
                BrushSessionDurationMinutes = DefaultProfileState.DefaultBrushSessionDurationMinutes,
                PetData = new Pet
                {
                    Name = dto.petName,
                    lastEatTime = -1,
                    eatCount = 0,
                    lastBrushTime = -1,
                    EyesItemId = DefaultProfileState.DefaultPetEyesItemId,
                    SkinItemId = DefaultProfileState.DefaultPetSkinItemId,
                    HatItemId = DefaultProfileState.DefaultPetHatItemId,
                    DressItemId = DefaultProfileState.DefaultPetDressItemId
                },
                RoomData = new Room(),
                InventoryData = DefaultProfileState.CreateInventory(),
                PendingReward = DefaultProfileState.InitialPendingReward,
                Muted = false
            };

        private string TryParseErrorMessage(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return "Request failed.";
            }

            try
            {
                ApiErrorResponse errorResponse = payloadCodec_.Deserialize<ApiErrorResponse>(payload);
                return errorResponse?.ResolveMessage() ?? payload;
            }
            catch
            {
                return payload;
            }
        }

        private string ResolveRemoteErrorMessage(RemoteResponse response, string fallbackMessage)
        {
            if (response == null)
            {
                return fallbackMessage;
            }

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
            {
                return response.ErrorMessage;
            }

            string parsedBodyMessage = TryParseErrorMessage(response.Body);
            if (!string.IsNullOrWhiteSpace(parsedBodyMessage) &&
                !string.Equals(parsedBodyMessage, "Request failed.", StringComparison.Ordinal))
            {
                return parsedBodyMessage;
            }

            return fallbackMessage;
        }

        private static string EscapeJson(string value) =>
            (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        [Serializable]
        private sealed class ChildProfileListEnvelope
        {
            public ChildProfileDto[] children;
        }

        [Serializable]
        private sealed class RewardClaimResponseEnvelope
        {
            public RewardClaimDto[] rewards;
        }

        [Serializable]
        private sealed class ChildProfileDto
        {
            public string id;
            public string familyId;
            public string name;
            public string petName;
            public int pictureId;
            public bool isActive;
        }

        [Serializable]
        private sealed class RewardClaimDto
        {
            public int kind;
            public int rewardType;
            public int currencyType;
            public int id;
            public int quantity;
        }

        [Serializable]
        private sealed class CreateInGameMarketPurchaseRequest
        {
            public int ItemType;
            public int ItemId;
        }


        [Serializable]
        private sealed class UpdateChildGameStateRequest
        {
            public string BaseRevision;
            public int BrushSessionDurationMinutes;
            public bool PendingReward;
            public bool Muted;
            public Pet PetState;
            public Room RoomState;
            public Inventory InventoryState;
        }

        [Serializable]
        private sealed class UpdateChildProfileRequest
        {
            public string Name;
            public string PetName;
            public int PictureId;
            public bool IsActive;
        }
    }
}
