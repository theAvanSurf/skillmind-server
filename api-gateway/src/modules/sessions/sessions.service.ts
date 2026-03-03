import { Injectable } from "@nestjs/common";
import { API_ENDPOINTS } from "../../common/endpoints";
import httpClient from "../../config/baseHttpClient";
import { DeviceDto, SessionDto } from "./sessions.dto";
import { ProfilesDto } from "../profiles/profiles.dto";

@Injectable()
export class SessionsService {
    async getSession(token: string): Promise<SessionDto> {
        return await httpClient.get<SessionDto>(
            API_ENDPOINTS.CORE.SESSIONS,
            { headers: { Authorization: token } }
        ) as unknown as SessionDto;
    }

    async updateSession(body: SessionDto, token: string): Promise<SessionDto> {
        return await httpClient.put<SessionDto>(
            API_ENDPOINTS.CORE.SESSIONS,
            body,
            { headers: { Authorization: token } }
        ) as unknown as SessionDto;
    }

    async removeSession(token: string): Promise<void> {
        await httpClient.delete(
            API_ENDPOINTS.CORE.SESSIONS,
            { headers: { Authorization: token } }
        );
    }

    async addDevice(body: DeviceDto, token: string): Promise<SessionDto> {
        return await httpClient.post<SessionDto>(
            API_ENDPOINTS.CORE.SESSIONS_DEVICES,
            body,
            { headers: { Authorization: token } }
        ) as unknown as SessionDto;
    }

    async removeDevice(deviceId: string, token: string): Promise<SessionDto> {
        return await httpClient.delete<SessionDto>(
            API_ENDPOINTS.CORE.SESSIONS_DEVICE_BY_ID(deviceId),
            { headers: { Authorization: token } }
        ) as unknown as SessionDto;
    }

    async addProfile(body: ProfilesDto, token: string): Promise<SessionDto> {
        return await httpClient.post<SessionDto>(
            API_ENDPOINTS.CORE.SESSIONS_PROFILES,
            body,
            { headers: { Authorization: token } }
        ) as unknown as SessionDto;
    }

    async removeProfile(profileId: string, token: string): Promise<SessionDto> {
        return await httpClient.delete<SessionDto>(
            API_ENDPOINTS.CORE.SESSIONS_PROFILE_BY_ID(profileId),
            { headers: { Authorization: token } }
        ) as unknown as SessionDto;
    }
}
