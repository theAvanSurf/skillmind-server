import { Injectable } from "@nestjs/common";
import { API_ENDPOINTS } from "../../common/endpoints";
import httpClient from "../../config/baseHttpClient";
import { CreateProfileDto, UpdateProfileDto, ProfilesQueryDto, ProfilesDto } from "./profiles.dto";

@Injectable()
export class ProfilesService {
    async createProfiles(body: CreateProfileDto[], token: string): Promise<ProfilesDto[]> {
        return await httpClient.post<ProfilesDto[]>(
            API_ENDPOINTS.CORE.PROFILES,
            body,
            { headers: { Authorization: token } }
        ) as unknown as ProfilesDto[];
    }

    async getProfiles(query: ProfilesQueryDto, token: string): Promise<ProfilesDto[]> {
        return await httpClient.get<ProfilesDto[]>(
            API_ENDPOINTS.CORE.PROFILES,
            {
                params: query,
                headers: { Authorization: token }
            }
        ) as unknown as ProfilesDto[];
    }

    async deleteProfile(profileId: string, token: string): Promise<any> {
        return await httpClient.delete<any>(
            API_ENDPOINTS.CORE.PROFILES_BY_ID(profileId),
            { headers: { Authorization: token } }
        );
    }

    async editProfile(profileId: string, body: UpdateProfileDto, token: string): Promise<ProfilesDto> {
        return await httpClient.patch<ProfilesDto>(
            API_ENDPOINTS.CORE.PROFILES_BY_ID(profileId),
            body,
            { headers: { Authorization: token } }
        ) as unknown as ProfilesDto;
    }
}
