import { Body, Controller, Delete, Get, Headers, Param, Patch, Post, Query } from "@nestjs/common";
import { ApiBearerAuth, ApiBody, ApiParam, ApiQuery, ApiResponse, ApiTags } from "@nestjs/swagger";
import { ProfilesService } from "./profiles.service";
import { CreateProfileDto, ProfilesDto, ProfilesQueryDto, UpdateProfileDto } from "./profiles.dto";

@ApiTags('Profiles')
@ApiBearerAuth()
@Controller('profiles')
export class ProfilesController {
    constructor(private readonly profilesService: ProfilesService) { }

    @Post()
    @ApiBody({ type: [CreateProfileDto] })
    @ApiResponse({ status: 201, description: 'Profiles created successfully' })
    @ApiResponse({ status: 400, description: 'At least one profile must be provided' })
    async createProfiles(
        @Body() body: CreateProfileDto[],
        @Headers('authorization') token: string
    ): Promise<ProfilesDto[]> {
        return this.profilesService.createProfiles(body, token);
    }

    @Get()
    @ApiQuery({ name: 'page', required: false, type: Number })
    @ApiQuery({ name: 'pageSize', required: false, type: Number })
    @ApiQuery({ name: 'search', required: false, type: String })
    @ApiQuery({ name: 'sortBy', required: false, type: String })
    @ApiResponse({ status: 200, description: 'List of user profiles' })
    async getProfiles(
        @Query() query: ProfilesQueryDto,
        @Headers('authorization') token: string
    ): Promise<ProfilesDto[]> {
        return this.profilesService.getProfiles(query, token);
    }

    @Delete(':profileId')
    @ApiParam({ name: 'profileId', type: String, description: 'UUID of the profile to delete' })
    @ApiResponse({ status: 200, description: 'Profile deleted successfully' })
    @ApiResponse({ status: 404, description: 'Profile not found' })
    async deleteProfile(
        @Param('profileId') profileId: string,
        @Headers('authorization') token: string
    ): Promise<any> {
        return this.profilesService.deleteProfile(profileId, token);
    }

    @Patch(':profileId')
    @ApiParam({ name: 'profileId', type: String, description: 'UUID of the profile to update' })
    @ApiBody({ type: UpdateProfileDto })
    @ApiResponse({ status: 200, description: 'Profile updated successfully' })
    @ApiResponse({ status: 404, description: 'Profile not found' })
    async editProfile(
        @Param('profileId') profileId: string,
        @Body() body: UpdateProfileDto,
        @Headers('authorization') token: string
    ): Promise<ProfilesDto> {
        return this.profilesService.editProfile(profileId, body, token);
    }
}
