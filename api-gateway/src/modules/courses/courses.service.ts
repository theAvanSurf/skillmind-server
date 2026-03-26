import { Injectable } from '@nestjs/common';
import httpClient from '../../config/baseHttpClient';
import { API_ENDPOINTS } from '../../common/endpoints';
import { CourseDto, CourseCardDto, CourseProgressDto, UpdateProgressDto, CreateCourseDto, CreateSeasonDto, CreateLessonDto, SeasonDto, LessonDto } from './courses.dto';

@Injectable()
export class CoursesService {
    async getCourseDetails(courseId: string, token: string): Promise<CourseDto> {
        return await httpClient.get<CourseDto>(API_ENDPOINTS.CORE.COURSES_BY_ID(courseId), { headers: { Authorization: token } }) as unknown as CourseDto;
    }
    async getRelatedCourses(courseId: string, token: string): Promise<CourseCardDto[]> {
        return await httpClient.get<CourseCardDto[]>(API_ENDPOINTS.CORE.COURSES_RELATED(courseId), { headers: { Authorization: token } }) as unknown as CourseCardDto[];
    }
    async updateProgress(courseId: string, body: UpdateProgressDto, token: string): Promise<CourseProgressDto> {
        return await httpClient.post<CourseProgressDto>(API_ENDPOINTS.CORE.COURSES_PROGRESS(courseId), body, { headers: { Authorization: token } }) as unknown as CourseProgressDto;
    }
    async createCourse(body: CreateCourseDto, token: string): Promise<CourseDto> {
        return await httpClient.post<CourseDto>(API_ENDPOINTS.CORE.COURSES, body, { headers: { Authorization: token } }) as unknown as CourseDto;
    }
    async createSeason(body: CreateSeasonDto, token: string): Promise<SeasonDto> {
        return await httpClient.post<SeasonDto>(API_ENDPOINTS.CORE.COURSES_SEASONS, body, { headers: { Authorization: token } }) as unknown as SeasonDto;
    }
    async createLesson(body: CreateLessonDto, token: string): Promise<LessonDto> {
        return await httpClient.post<LessonDto>(API_ENDPOINTS.CORE.COURSES_LESSONS, body, { headers: { Authorization: token } }) as unknown as LessonDto;
    }
}