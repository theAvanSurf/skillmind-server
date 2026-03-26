import { Controller, Get, Post, Param, Body, Headers, UseGuards } from '@nestjs/common';
import { ApiBearerAuth, ApiTags } from '@nestjs/swagger';
import { CoursesService } from './courses.service';
import { UpdateProgressDto, CreateCourseDto, CreateSeasonDto, CreateLessonDto } from './courses.dto';

@ApiTags('Courses')
@ApiBearerAuth()
@Controller('courses')
export class CoursesController {
    constructor(private readonly coursesService: CoursesService) {}

    @Get(':courseId')
    getCourseDetails(
        @Param('courseId') courseId: string,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.getCourseDetails(courseId, token);
    }

    @Get(':courseId/related')
    getRelatedCourses(
        @Param('courseId') courseId: string,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.getRelatedCourses(courseId, token);
    }

    @Post(':courseId/progress')
    updateProgress(
        @Param('courseId') courseId: string,
        @Body() dto: UpdateProgressDto,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.updateProgress(courseId, dto, token);
    }

    @Post()
    createCourse(
        @Body() dto: CreateCourseDto,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.createCourse(dto, token);
    }

    @Post('seasons')
    createSeason(
        @Body() dto: CreateSeasonDto,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.createSeason(dto, token);
    }

    @Post('lessons')
    createLesson(
        @Body() dto: CreateLessonDto,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.createLesson(dto, token);
    }
}