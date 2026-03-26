import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

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
    @ApiProperty() lastLessonId: string;
    @ApiProperty() progressPercent: number;
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
    @ApiProperty() title: string;
    @ApiProperty() description: string;
    @ApiProperty() thumbnailUrl: string;
    @ApiPropertyOptional() category?: string;
    @ApiPropertyOptional() tags?: string;
}

export class CreateSeasonDto {
    @ApiProperty() courseId: string;
    @ApiProperty() title: string;
    @ApiProperty() order: number;
}

export class CreateLessonDto {
    @ApiProperty() seasonId: string;
    @ApiProperty() title: string;
    @ApiProperty() videoUrl: string;
    @ApiPropertyOptional() description?: string;
    @ApiProperty() order: number;
    @ApiProperty() durationSeconds: number;
}