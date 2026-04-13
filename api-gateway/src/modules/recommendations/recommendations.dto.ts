import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class TrackEventDto {
    @ApiProperty({ example: '550e8400-e29b-41d4-a716-446655440000' })
    profile_id!: string;

    @ApiProperty({ example: '550e8400-e29b-41d4-a716-446655440001' })
    course_id!: string;

    @ApiProperty({ description: 'started | completed | paused | clicked | searched' })
    event_type!: string;

    @ApiPropertyOptional() category?: string;
    @ApiPropertyOptional() tags?: string;
    @ApiPropertyOptional() search_query?: string;
    @ApiPropertyOptional() engagement_seconds?: number;
}

export class RecommendedCourseDto {
    @ApiProperty() id!: string;
    @ApiProperty() title!: string;
    @ApiProperty() thumbnail_url!: string;
    @ApiPropertyOptional() category?: string;
    @ApiPropertyOptional() tags?: string;
    @ApiProperty() relevance_score!: number;
    @ApiPropertyOptional({ example: 'Because you watched Music Production' })
    reason?: string;
}

export class RecommendationResponseDto {
    @ApiProperty() profile_id!: string;
    @ApiProperty() is_personalized!: boolean;
    @ApiProperty({ type: [RecommendedCourseDto] })
    recommendations!: RecommendedCourseDto[];
}