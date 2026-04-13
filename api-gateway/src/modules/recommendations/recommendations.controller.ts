import { Body, Controller, Get, Param, Post, UseGuards } from '@nestjs/common';
import { ApiBearerAuth, ApiBody, ApiParam, ApiResponse, ApiTags } from '@nestjs/swagger';
import { RecommendationsService } from './recommendations.service';
import { TrackEventDto, RecommendationResponseDto, RecommendedCourseDto } from './recommendations.dto';
import { AuthGuard } from '../../common/guards/auth.guard';

@ApiTags('Recommendations')
@Controller('recommendations')
export class RecommendationsController {
    constructor(private readonly recommendationsService: RecommendationsService) {}

    @Get(':profileId')
    @ApiBearerAuth()
    @UseGuards(AuthGuard)
    @ApiParam({ name: 'profileId', type: String })
    @ApiResponse({ status: 200, type: RecommendationResponseDto })
    async getRecommendations(@Param('profileId') profileId: string): Promise<RecommendationResponseDto> {
        return this.recommendationsService.getRecommendations(profileId);
    }

    @Post('events/track')
    @ApiBearerAuth()
    @UseGuards(AuthGuard)
    @ApiBody({ type: TrackEventDto })
    @ApiResponse({ status: 201, description: 'Event tracked' })
    async trackEvent(@Body() dto: TrackEventDto): Promise<{ success: boolean; message: string }> {
        return this.recommendationsService.trackEvent(dto);
    }

    @Get('trending/courses')
    @ApiResponse({ status: 200, type: [RecommendedCourseDto] })
    async getTrending(): Promise<RecommendedCourseDto[]> {
        return this.recommendationsService.getTrending();
    }
}