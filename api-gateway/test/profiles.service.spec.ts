import { ProfilesService } from '../src/modules/profiles/profiles.service';
import httpClient from '../src/config/baseHttpClient';
import { API_ENDPOINTS } from '../src/common/endpoints';
import { CreateProfileDto, UpdateProfileDto, ProfilesQueryDto } from '../src/modules/profiles/profiles.dto';

// Mock httpClient
jest.mock('../src/config/baseHttpClient', () => ({
    __esModule: true,
    default: {
        post: jest.fn(),
        get: jest.fn(),
        delete: jest.fn(),
        patch: jest.fn(),
    },
}));

describe('ProfilesService', () => {
    let profilesService: ProfilesService;
    const mockHttpClient = httpClient as jest.Mocked<typeof httpClient>;
    const token = 'Bearer jwt-token';

    beforeEach(() => {
        profilesService = new ProfilesService();
        jest.clearAllMocks();
    });

    describe('createProfiles', () => {
        const createProfilesRequest: CreateProfileDto[] = [
            {
                name: 'Work Profile',
                avatar: 'https://example.com/avatar1.png',
            },
            {
                name: 'Personal Profile',
                avatar: 'https://example.com/avatar2.png',
            },
        ];

        it('should call httpClient.post with correct endpoint and payload', async () => {
            const mockResponse = [
                { id: '1', name: 'Work Profile', avatar: 'https://example.com/avatar1.png' },
                { id: '2', name: 'Personal Profile', avatar: 'https://example.com/avatar2.png' },
            ];
            mockHttpClient.post.mockResolvedValue(mockResponse);

            const result = await profilesService.createProfiles(createProfilesRequest, token);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.PROFILES,
                createProfilesRequest,
                { headers: { Authorization: token } }
            );
            expect(result).toEqual(mockResponse);
        });

        it('should handle empty array', async () => {
            mockHttpClient.post.mockResolvedValue([]);

            const result = await profilesService.createProfiles([], token);

            expect(result).toEqual([]);
        });

        it('should propagate errors from httpClient', async () => {
            mockHttpClient.post.mockRejectedValue(new Error('Network error'));

            await expect(
                profilesService.createProfiles(createProfilesRequest, token)
            ).rejects.toThrow('Network error');
        });
    });

    describe('getProfiles', () => {
        const query: ProfilesQueryDto = {
            userId: 'user-123',
        };

        it('should call httpClient.get with correct endpoint and params', async () => {
            const mockResponse = [
                { id: '1', name: 'Work Profile', userId: 'user-123' },
                { id: '2', name: 'Personal Profile', userId: 'user-123' },
            ];
            mockHttpClient.get.mockResolvedValue(mockResponse);

            const result = await profilesService.getProfiles(query, token);

            expect(mockHttpClient.get).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.PROFILES,
                {
                    params: query,
                    headers: { Authorization: token },
                }
            );
            expect(result).toEqual(mockResponse);
        });

        it('should return empty array when no profiles found', async () => {
            mockHttpClient.get.mockResolvedValue([]);

            const result = await profilesService.getProfiles(query, token);

            expect(result).toEqual([]);
        });

        it('should handle query without filters', async () => {
            const emptyQuery: ProfilesQueryDto = {};
            mockHttpClient.get.mockResolvedValue([]);

            await profilesService.getProfiles(emptyQuery, token);

            expect(mockHttpClient.get).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.PROFILES,
                {
                    params: emptyQuery,
                    headers: { Authorization: token },
                }
            );
        });
    });

    describe('deleteProfile', () => {
        const profileId = 'profile-123';

        it('should call httpClient.delete with correct endpoint', async () => {
            const mockResponse = { success: true };
            mockHttpClient.delete.mockResolvedValue(mockResponse);

            const result = await profilesService.deleteProfile(profileId, token);

            expect(mockHttpClient.delete).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.PROFILES_BY_ID(profileId),
                { headers: { Authorization: token } }
            );
            expect(result).toEqual(mockResponse);
        });

        it('should propagate error when profile not found', async () => {
            mockHttpClient.delete.mockRejectedValue(new Error('Profile not found'));

            await expect(
                profilesService.deleteProfile(profileId, token)
            ).rejects.toThrow('Profile not found');
        });
    });

    describe('editProfile', () => {
        const profileId = 'profile-123';
        const updateData: UpdateProfileDto = {
            name: 'Updated Profile Name',
        };

        it('should call httpClient.patch with correct endpoint and payload', async () => {
            const mockResponse = {
                id: profileId,
                name: 'Updated Profile Name',
                avatar: 'https://example.com/avatar.png',
            };
            mockHttpClient.patch.mockResolvedValue(mockResponse);

            const result = await profilesService.editProfile(profileId, updateData, token);

            expect(mockHttpClient.patch).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.PROFILES_BY_ID(profileId),
                updateData,
                { headers: { Authorization: token } }
            );
            expect(result).toEqual(mockResponse);
        });

        it('should handle partial updates', async () => {
            const partialUpdate: UpdateProfileDto = {
                avatar: 'https://example.com/new-avatar.png',
            };
            mockHttpClient.patch.mockResolvedValue({
                id: profileId,
                name: 'Original Name',
                avatar: 'https://example.com/new-avatar.png',
            });

            const result = await profilesService.editProfile(profileId, partialUpdate, token);

            expect(mockHttpClient.patch).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.PROFILES_BY_ID(profileId),
                partialUpdate,
                { headers: { Authorization: token } }
            );
            expect(result.avatar).toBe('https://example.com/new-avatar.png');
        });

        it('should propagate error when profile not found', async () => {
            mockHttpClient.patch.mockRejectedValue(new Error('Profile not found'));

            await expect(
                profilesService.editProfile(profileId, updateData, token)
            ).rejects.toThrow('Profile not found');
        });
    });
});