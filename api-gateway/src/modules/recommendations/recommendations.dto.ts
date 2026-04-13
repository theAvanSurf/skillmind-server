import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';
import { IsString, IsUUID, IsOptional, IsNumber, IsNotEmpty } from 'class-validator';

export class TrackEventDto {
    @ApiProperty({ example: '550e8400-e29b-41d4-a716-446655440000' })
    @IsUUID()
    @IsNotEmpty()
    profile_id!: string;

    @ApiProperty({ example: '550e8400-e29b-41d4-a716-446655440001' })
    @IsString()
    @IsNotEmpty()
    course_id!: string;

    @ApiProperty({ description: 'started | completed | paused | clicked | searched' })
    @IsString()
    @IsNotEmpty()
    event_type!: string;

    @ApiPropertyOptional() @IsOptional() @IsString() category?: string;
    @ApiPropertyOptional() @IsOptional() @IsString() tags?: string;
    @ApiPropertyOptional() @IsOptional() @IsString() search_query?: string;
    @ApiPropertyOptional() @IsOptional() @IsNumber() engagement_seconds?: number;
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