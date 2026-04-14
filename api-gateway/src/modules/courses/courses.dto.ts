import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';
import { IsString, IsNumber, IsOptional, IsUUID, IsBoolean } from 'class-validator';
import { Type } from 'class-transformer';

export class LessonDto {
    @ApiProperty() id: string;
    @ApiProperty() seasonId: string;
    @ApiProperty() title: string;
    @ApiProperty() videoUrl: string;
    @ApiPropertyOptional() description?: string;
    @ApiProperty() order: number;
    @ApiProperty() durationSeconds: number;
}

export class SeasonDto {
    @ApiProperty() id: string;
    @ApiProperty() courseId: string;
    @ApiProperty() title: string;
    @ApiProperty() order: number;
    @ApiProperty({ type: [LessonDto] }) lessons: LessonDto[];
}

export class CourseDto {
    @ApiProperty() id: string;
    @ApiProperty() title: string;
    @ApiProperty() description: string;
    @ApiProperty() thumbnailUrl: string;
    @ApiPropertyOptional() category?: string;
    @ApiPropertyOptional() tags?: string;
    @ApiProperty({ type: [SeasonDto] }) seasons: SeasonDto[];
}

export class CourseCardDto {
    @ApiProperty() id: string;
    @ApiProperty() title: string;
    @ApiProperty() thumbnailUrl: string;
    @ApiPropertyOptional() category?: string;
    @ApiPropertyOptional() progressPercent?: number;
}

export class UpdateProgressDto {
    @ApiProperty() @IsUUID() lastLessonId: string;
    @ApiProperty() @IsNumber() progressPercent: number;
}

export class CourseProgressDto {
    @ApiProperty() id: string;
    @ApiProperty() profileId: string;
    @ApiProperty() courseId: string;
    @ApiPropertyOptional() lastLessonId?: string;
    @ApiProperty() progressPercent: number;
    @ApiProperty() updatedOn: string;
}

export class CreateCourseDto {
    @ApiProperty() @IsString() title: string;
    @ApiProperty() @IsString() description: string;
    @ApiProperty() @IsString() thumbnailUrl: string;
    @ApiPropertyOptional() @IsOptional() @IsString() category?: string;
    @ApiPropertyOptional() @IsOptional() @IsString() tags?: string;
}

export class BrowseCoursesQueryDto {
    @ApiPropertyOptional() @IsOptional() @IsString() search?: string;
    @ApiPropertyOptional() @IsOptional() @IsString() category?: string;
    @ApiPropertyOptional() @IsOptional() @IsBoolean() @Type(() => Boolean) freeOnly?: boolean;
    @ApiPropertyOptional() @IsOptional() @Type(() => Number) page?: number;
    @ApiPropertyOptional() @IsOptional() @Type(() => Number) pageSize?: number;
    @ApiPropertyOptional() @IsOptional() @IsString() sort?: string;
}

export class BrowseCourseDto {
    @ApiProperty() id: string;
    @ApiProperty() title: string;
    @ApiProperty() description: string;
    @ApiProperty() thumbnailUrl: string;
    @ApiPropertyOptional() category?: string;
    @ApiPropertyOptional() tags?: string;
    @ApiProperty() price: number;
    @ApiProperty() totalSeasons: number;
    @ApiProperty() totalLessons: number;
    @ApiProperty() createdOn: string;
}

export class BrowseCoursesResultDto {
    @ApiProperty({ type: [BrowseCourseDto] }) courses: BrowseCourseDto[];
    @ApiProperty() totalCount: number;
    @ApiProperty() page: number;
    @ApiProperty() pageSize: number;
    @ApiProperty() hasMore: boolean;
}

export class CourseSearchSuggestionDto {
    @ApiProperty() text: string;
    @ApiProperty() type: string;
    @ApiPropertyOptional() courseId?: string;
}

export class CreateSeasonDto {
    @ApiProperty() @IsUUID() courseId: string;
    @ApiProperty() @IsString() title: string;
    @ApiProperty() @IsNumber() order: number;
}

export class CreateLessonDto {
    @ApiProperty() @IsUUID() seasonId: string;
    @ApiProperty() @IsString() title: string;
    @ApiProperty() @IsString() videoUrl: string;
    @ApiPropertyOptional() @IsOptional() @IsString() description?: string;
    @ApiProperty() @IsNumber() order: number;
    @ApiProperty() @IsNumber() durationSeconds: number;
}