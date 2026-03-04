import { Body, Controller, Delete, Get, Headers, Param, Post, Put, UseGuards } from "@nestjs/common";
import { ApiBearerAuth, ApiBody, ApiParam, ApiResponse, ApiTags } from "@nestjs/swagger";
import { SessionsService } from "./sessions.service";
import { DeviceDto } from "./sessions.dto";
import type { SessionDto } from "./sessions.dto";
import type { ProfilesDto } from "../profiles/profiles.dto";
import { AuthGuard } from "../../common/guards/auth.guard";

@ApiTags('Sessions')
@ApiBearerAuth()
@UseGuards(AuthGuard)
@Controller('sessions')
export class SessionsController {
    constructor(private readonly sessionsService: SessionsService) { }

    @Get()
    @ApiResponse({ status: 200, description: 'Active session for the authenticated user' })
    @ApiResponse({ status: 404, description: 'No active session found' })
    async getSession(
        @Headers('authorization') token: string
    ): Promise<SessionDto> {
        return this.sessionsService.getSession(token);
    }

    @Put()
    @ApiBody({ type: Object, description: 'Session update payload' })
    @ApiResponse({ status: 200, description: 'Session updated' })
    async updateSession(
        @Body() body: Record<string, any>,
        @Headers('authorization') token: string
    ): Promise<SessionDto> {
        return this.sessionsService.updateSession(body as SessionDto, token);
    }

    @Delete()
    @ApiResponse({ status: 204, description: 'Session removed' })
    @ApiResponse({ status: 404, description: 'No active session found' })
    async removeSession(
        @Headers('authorization') token: string
    ): Promise<void> {
        return this.sessionsService.removeSession(token);
    }

    @Post('devices')
    @ApiBody({ type: DeviceDto })
    @ApiResponse({ status: 200, description: 'Device added to session' })
    async addDevice(
        @Body() body: DeviceDto,
        @Headers('authorization') token: string
    ): Promise<SessionDto> {
        return this.sessionsService.addDevice(body, token);
    }

    @Delete('devices/:deviceId')
    @ApiParam({ name: 'deviceId', type: String, description: 'ID of the device to remove' })
    @ApiResponse({ status: 200, description: 'Device removed from session' })
    async removeDevice(
        @Param('deviceId') deviceId: string,
        @Headers('authorization') token: string
    ): Promise<SessionDto> {
        return this.sessionsService.removeDevice(deviceId, token);
    }

    @Post('profiles')
    @ApiBody({ type: Object, description: 'Profile to add to session' })
    @ApiResponse({ status: 200, description: 'Profile added to session' })
    async addProfile(
        @Body() body: Record<string, any>,
        @Headers('authorization') token: string
    ): Promise<SessionDto> {
        return this.sessionsService.addProfile(body as ProfilesDto, token);
    }

    @Delete('profiles/:profileId')
    @ApiParam({ name: 'profileId', type: String, description: 'UUID of the profile to remove from session' })
    @ApiResponse({ status: 200, description: 'Profile removed from session' })
    async removeProfile(
        @Param('profileId') profileId: string,
        @Headers('authorization') token: string
    ): Promise<SessionDto> {
        return this.sessionsService.removeProfile(profileId, token);
    }
}
