import { Controller, Get, Post, Param, Body, Headers, Query } from '@nestjs/common';
import { ApiBearerAuth, ApiTags } from '@nestjs/swagger';
import { CoursesService } from './courses.service';
import { UpdateProgressDto, CreateCourseDto, CreateSeasonDto, CreateLessonDto, BrowseCoursesQueryDto } from './courses.dto';

@ApiTags('Courses')
@ApiBearerAuth()
@Controller('courses')
export class CoursesController {
    constructor(private readonly coursesService: CoursesService) {}

    // ── Static routes first (before :courseId param) ──────────────────────────

    @Get('search')
    getSearchSuggestions(
        @Query('q') q: string,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.getSearchSuggestions(q, token);
    }

    @Get('categories')
    getCategories(@Headers('authorization') token: string) {
        return this.coursesService.getCategories(token);
    }

    // ── Browse (GET /courses with query params) ───────────────────────────────

    @Get()
    browseCourses(
        @Query() query: BrowseCoursesQueryDto,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.browseCourses(query, token);
    }

    // ── Param routes ──────────────────────────────────────────────────────────

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

    @Get(':courseId/enrollment-status')
    getEnrollmentStatus(
        @Param('courseId') courseId: string,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.getEnrollmentStatus(courseId, token);
    }

    @Post(':courseId/purchase')
    createPurchaseIntent(
        @Param('courseId') courseId: string,
        @Headers('authorization') token: string,
    ) {
        return this.coursesService.createPurchaseIntent(courseId, token);
    }

    @Get(':courseId/live')
    getActiveLiveSession(@Param('courseId') courseId: string) {
        return this.coursesService.getActiveLiveSession(courseId);
    }
}
