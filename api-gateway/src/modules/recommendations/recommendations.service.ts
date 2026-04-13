import { Injectable, HttpException } from '@nestjs/common';
import axios from 'axios';
import { appConfig } from '../../config/settings';
import { TrackEventDto, RecommendationResponseDto, RecommendedCourseDto } from './recommendations.dto';

const recommendationClient = axios.create({
    baseURL: appConfig.API_RECOMMENDATION_URL,
    headers: { 'Content-Type': 'application/json' },
    timeout: 5000,
});

recommendationClient.interceptors.response.use(
    (res) => res.data,
    (err) => {
        const status = err.response?.status || 500;
        const message = err.response?.data?.detail || err.message || 'Recommendation service error';
        throw new HttpException({ statusCode: status, message }, status);
    }
);

@Injectable()
export class RecommendationsService {
    async getRecommendations(profileId: string): Promise<RecommendationResponseDto> {
        return await recommendationClient.get(`/recommendations/${profileId}`) as unknown as RecommendationResponseDto;
    }

    async trackEvent(dto: TrackEventDto): Promise<{ success: boolean; message: string }> {
        return await recommendationClient.post('/recommendations/events/track', dto) as unknown as { success: boolean; message: string };
    }

    async getTrending(): Promise<RecommendedCourseDto[]> {
        return await recommendationClient.get('/recommendations/cold-start/trending') as unknown as RecommendedCourseDto[];
    }
}