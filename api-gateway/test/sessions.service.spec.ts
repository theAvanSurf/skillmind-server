import { SessionsService } from '../src/modules/sessions/sessions.service';
import httpClient from '../src/config/baseHttpClient';
import { API_ENDPOINTS } from '../src/common/endpoints';
import { DeviceDto, SessionDto } from '../src/modules/sessions/sessions.dto';
import { ProfilesDto } from '../src/modules/profiles/profiles.dto';

// Mock httpClient
jest.mock('../src/config/baseHttpClient', () => ({
    __esModule: true,
    default: {
        get: jest.fn(),
        put: jest.fn(),
        post: jest.fn(),
        delete: jest.fn(),
    },
}));

describe('SessionsService', () => {
    let sessionsService: SessionsService;
    const mockHttpClient = httpClient as jest.Mocked<typeof httpClient>;
    const token = 'Bearer jwt-token';

    beforeEach(() => {
        sessionsService = new SessionsService();
        jest.clearAllMocks();
    });

    describe('getSession', () => {
        it('should call httpClient.get with correct endpoint and token', async () => {
            const mockSession: SessionDto = {
                id: 'session-123',
                userId: 'user-123',
                devices: [],
                profiles: [],
                createdAt: '2026-03-26T00:00:00Z',
            };
            mockHttpClient.get.mockResolvedValue(mockSession);

            const result = await sessionsService.getSession(token);

            expect(mockHttpClient.get).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.SESSIONS,
                { headers: { Authorization: token } }
            );
            expect(result).toEqual(mockSession);
        });

        it('should propagate error when session not found', async () => {
            mockHttpClient.get.mockRejectedValue(new Error('Session not found'));

            await expect(sessionsService.getSession(token)).rejects.toThrow('Session not found');
        });
    });

    describe('updateSession', () => {
        const sessionData: SessionDto = {
            id: 'session-123',
            userId: 'user-123',
            devices: [],
            profiles: [],
            createdAt: '2026-03-26T00:00:00Z',
        };

        it('should call httpClient.put with correct endpoint and payload', async () => {
            mockHttpClient.put.mockResolvedValue(sessionData);

            const result = await sessionsService.updateSession(sessionData, token);

            expect(mockHttpClient.put).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.SESSIONS,
                sessionData,
                { headers: { Authorization: token } }
            );
            expect(result).toEqual(sessionData);
        });

        it('should propagate errors', async () => {
            mockHttpClient.put.mockRejectedValue(new Error('Update failed'));

            await expect(
                sessionsService.updateSession(sessionData, token)
            ).rejects.toThrow('Update failed');
        });
    });

    describe('removeSession', () => {
        it('should call httpClient.delete with correct endpoint', async () => {
            mockHttpClient.delete.mockResolvedValue(undefined);

            await sessionsService.removeSession(token);

            expect(mockHttpClient.delete).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.SESSIONS,
                { headers: { Authorization: token } }
            );
        });

        it('should propagate errors', async () => {
            mockHttpClient.delete.mockRejectedValue(new Error('Delete failed'));

            await expect(sessionsService.removeSession(token)).rejects.toThrow('Delete failed');
        });
    });

    describe('addDevice', () => {
        const device: DeviceDto = {
            id: 'device-123',
            name: 'iPhone 15',
            type: 'mobile',
            lastUsed: '2026-03-26T00:00:00Z',
        };

        it('should call httpClient.post with correct endpoint and payload', async () => {
            const mockSession: SessionDto = {
                id: 'session-123',
                userId: 'user-123',
                devices: [device],
                profiles: [],
                createdAt: '2026-03-26T00:00:00Z',
            };
            mockHttpClient.post.mockResolvedValue(mockSession);

            const result = await sessionsService.addDevice(device, token);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.SESSIONS_DEVICES,
                device,
                { headers: { Authorization: token } }
            );
            expect(result.devices).toContainEqual(device);
        });

        it('should propagate errors', async () => {
            mockHttpClient.post.mockRejectedValue(new Error('Add device failed'));

            await expect(sessionsService.addDevice(device, token)).rejects.toThrow(
                'Add device failed'
            );
        });
    });

    describe('removeDevice', () => {
        const deviceId = 'device-123';

        it('should call httpClient.delete with correct endpoint', async () => {
            const mockSession: SessionDto = {
                id: 'session-123',
                userId: 'user-123',
                devices: [],
                profiles: [],
                createdAt: '2026-03-26T00:00:00Z',
            };
            mockHttpClient.delete.mockResolvedValue(mockSession);

            const result = await sessionsService.removeDevice(deviceId, token);

            expect(mockHttpClient.delete).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.SESSIONS_DEVICE_BY_ID(deviceId),
                { headers: { Authorization: token } }
            );
            expect(result.devices).toEqual([]);
        });

        it('should propagate errors when device not found', async () => {
            mockHttpClient.delete.mockRejectedValue(new Error('Device not found'));

            await expect(sessionsService.removeDevice(deviceId, token)).rejects.toThrow(
                'Device not found'
            );
        });
    });

    describe('addProfile', () => {
        const profile: ProfilesDto = {
            id: 'profile-123',
            name: 'Work Profile',
            avatar: 'https://example.com/avatar.png',
        };

        it('should call httpClient.post with correct endpoint and payload', async () => {
            const mockSession: SessionDto = {
                id: 'session-123',
                userId: 'user-123',
                devices: [],
                profiles: [profile],
                createdAt: '2026-03-26T00:00:00Z',
            };
            mockHttpClient.post.mockResolvedValue(mockSession);

            const result = await sessionsService.addProfile(profile, token);

            expect(mockHttpClient.post).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.SESSIONS_PROFILES,
                profile,
                { headers: { Authorization: token } }
            );
            expect(result.profiles).toContainEqual(profile);
        });

        it('should propagate errors', async () => {
            mockHttpClient.post.mockRejectedValue(new Error('Add profile failed'));

            await expect(sessionsService.addProfile(profile, token)).rejects.toThrow(
                'Add profile failed'
            );
        });
    });

    describe('removeProfile', () => {
        const profileId = 'profile-123';

        it('should call httpClient.delete with correct endpoint', async () => {
            const mockSession: SessionDto = {
                id: 'session-123',
                userId: 'user-123',
                devices: [],
                profiles: [],
                createdAt: '2026-03-26T00:00:00Z',
            };
            mockHttpClient.delete.mockResolvedValue(mockSession);

            const result = await sessionsService.removeProfile(profileId, token);

            expect(mockHttpClient.delete).toHaveBeenCalledWith(
                API_ENDPOINTS.CORE.SESSIONS_PROFILE_BY_ID(profileId),
                { headers: { Authorization: token } }
            );
            expect(result.profiles).toEqual([]);
        });

        it('should propagate errors when profile not found', async () => {
            mockHttpClient.delete.mockRejectedValue(new Error('Profile not found'));

            await expect(sessionsService.removeProfile(profileId, token)).rejects.toThrow(
                'Profile not found'
            );
        });
    });
});