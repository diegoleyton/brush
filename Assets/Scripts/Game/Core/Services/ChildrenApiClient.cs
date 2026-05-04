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
                return Array.Empty<Profile>();
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

            ChildGameStateSnapshot snapshot = payloadCodec_.Deserialize<ChildGameStateSnapshot>(response.Body);
            logger_?.Log($"[GameStateApi] Loaded child game state for {remoteProfileId}: {DescribeSnapshot(snapshot)}");
            return snapshot;
        }

        public async Task<bool> UpdateGameStateAsync(string remoteProfileId, ChildGameStateSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(remoteProfileId) || snapshot == null)
            {
                return false;
            }

            string body = payloadCodec_.Serialize(new UpdateChildGameStateRequest
            {
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

            logger_?.Log($"[GameStateApi] Pushed child game state for {remoteProfileId}: {DescribeSnapshot(snapshot)}");
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
                logger_?.Log($"[Profiles] Remote request failed: {response.StatusCode} {errorMessage}");
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

        private static string EscapeJson(string value) =>
            (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        private static string DescribeSnapshot(ChildGameStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "snapshot=null";
            }

            return
                $"pendingReward={snapshot.PendingReward}, " +
                $"muted={snapshot.Muted}, " +
                $"brushMinutes={snapshot.BrushSessionDurationMinutes}, " +
                $"petName={(snapshot.PetState?.Name ?? "<null>")}, " +
                $"roomObjects={snapshot.RoomState?.PlaceableObjects?.Count ?? 0}, " +
                $"paintedSurfaces={snapshot.RoomState?.PaintedSurfaces?.Count ?? 0}, " +
                $"inventory={DescribeInventory(snapshot.InventoryState)}";
        }

        private static string DescribeInventory(Inventory inventory)
        {
            if (inventory == null)
            {
                return "<null>";
            }

            return
                $"placeable:{inventory.PlaceableObjects?.Count ?? 0}, " +
                $"paint:{inventory.Paint?.Count ?? 0}, " +
                $"food:{inventory.Food?.Count ?? 0}, " +
                $"skin:{inventory.Skin?.Count ?? 0}, " +
                $"hat:{inventory.Hat?.Count ?? 0}, " +
                $"dress:{inventory.Dress?.Count ?? 0}, " +
                $"eyes:{inventory.Eyes?.Count ?? 0}";
        }

        [Serializable]
        private sealed class ChildProfileListEnvelope
        {
            public ChildProfileDto[] children;
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
        private sealed class UpdateChildGameStateRequest
        {
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
