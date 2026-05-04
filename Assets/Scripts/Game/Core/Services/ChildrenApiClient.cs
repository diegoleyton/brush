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
    }
}
