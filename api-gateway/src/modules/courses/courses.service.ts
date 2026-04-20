import { Injectable } from '@nestjs/common';
import httpClient from '../../config/baseHttpClient';
import { API_ENDPOINTS } from '../../common/endpoints';
import {
    CourseDto, CourseCardDto, CourseProgressDto, UpdateProgressDto,
    CreateCourseDto, CreateSeasonDto, CreateLessonDto, SeasonDto, LessonDto,
    BrowseCoursesQueryDto, BrowseCoursesResultDto, CourseSearchSuggestionDto,
    EnrolledCourseDto,
} from './courses.dto';

@Injectable()
export class CoursesService {
    async browseCourses(query: BrowseCoursesQueryDto, token: string): Promise<BrowseCoursesResultDto> {
        const params = new URLSearchParams();
        if (query.search)    params.set('search', query.search);
        if (query.category)  params.set('category', query.category);
        if (query.freeOnly !== undefined) params.set('freeOnly', String(query.freeOnly));
        if (query.page !== undefined)     params.set('page', String(query.page));
        if (query.pageSize !== undefined) params.set('pageSize', String(query.pageSize));
        if (query.sort)      params.set('sort', query.sort);
        const qs = params.toString();
        return httpClient.get(`${API_ENDPOINTS.CORE.COURSES}${qs ? `?${qs}` : ''}`, { headers: { Authorization: token } }) as unknown as BrowseCoursesResultDto;
    }

    async getSearchSuggestions(q: string, token: string): Promise<CourseSearchSuggestionDto[]> {
        return httpClient.get(`${API_ENDPOINTS.CORE.COURSES_SEARCH}?q=${encodeURIComponent(q ?? '')}`, { headers: { Authorization: token } }) as unknown as CourseSearchSuggestionDto[];
    }

    async getCategories(token: string): Promise<string[]> {
        return httpClient.get(API_ENDPOINTS.CORE.COURSES_CATEGORIES, { headers: { Authorization: token } }) as unknown as string[];
    }

    async getCourseDetails(courseId: string, token: string): Promise<CourseDto> {
        return httpClient.get<CourseDto>(API_ENDPOINTS.CORE.COURSES_BY_ID(courseId), { headers: { Authorization: token } }) as unknown as CourseDto;
    }
    async getRelatedCourses(courseId: string, token: string): Promise<CourseCardDto[]> {
        return httpClient.get<CourseCardDto[]>(API_ENDPOINTS.CORE.COURSES_RELATED(courseId), { headers: { Authorization: token } }) as unknown as CourseCardDto[];
    }
    async getProgress(courseId: string, token: string): Promise<CourseProgressDto | null> {
        try {
            return await httpClient.get<CourseProgressDto>(API_ENDPOINTS.CORE.COURSES_PROGRESS(courseId), { headers: { Authorization: token } }) as unknown as CourseProgressDto;
        } catch {
            return null;
        }
    }

    async updateProgress(courseId: string, body: UpdateProgressDto, token: string): Promise<CourseProgressDto> {
        return httpClient.post<CourseProgressDto>(API_ENDPOINTS.CORE.COURSES_PROGRESS(courseId), body, { headers: { Authorization: token } }) as unknown as CourseProgressDto;
    }
    async createCourse(body: CreateCourseDto, token: string): Promise<CourseDto> {
        return httpClient.post<CourseDto>(API_ENDPOINTS.CORE.COURSES, body, { headers: { Authorization: token } }) as unknown as CourseDto;
    }
    async createSeason(body: CreateSeasonDto, token: string): Promise<SeasonDto> {
        return httpClient.post<SeasonDto>(API_ENDPOINTS.CORE.COURSES_SEASONS, body, { headers: { Authorization: token } }) as unknown as SeasonDto;
    }
    async createLesson(body: CreateLessonDto, token: string): Promise<LessonDto> {
        return httpClient.post<LessonDto>(API_ENDPOINTS.CORE.COURSES_LESSONS, body, { headers: { Authorization: token } }) as unknown as LessonDto;
    }

    async confirmEnrollment(courseId: string, paymentIntentId: string, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.COURSES_CONFIRM_ENROLLMENT(courseId), { paymentIntentId }, { headers: { Authorization: token } });
    }

    async getEnrolledCourses(token: string): Promise<EnrolledCourseDto[]> {
        return httpClient.get(API_ENDPOINTS.CORE.COURSES_MY_ENROLLMENTS, { headers: { Authorization: token } }) as unknown as EnrolledCourseDto[];
    }

    async getInProgressCourses(token: string): Promise<EnrolledCourseDto[]> {
        return httpClient.get(API_ENDPOINTS.CORE.COURSES_IN_PROGRESS, { headers: { Authorization: token } }) as unknown as EnrolledCourseDto[];
    }

    async getRecentlyWatched(token: string, limit = 20): Promise<EnrolledCourseDto[]> {
        return httpClient.get(`${API_ENDPOINTS.CORE.COURSES_RECENTLY_WATCHED}?limit=${limit}`, { headers: { Authorization: token } }) as unknown as EnrolledCourseDto[];
    }

    async getEnrollmentStatus(courseId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.COURSES_ENROLLMENT_STATUS(courseId), { headers: { Authorization: token } });
    }

    async createPurchaseIntent(courseId: string, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.COURSES_PURCHASE(courseId), {}, { headers: { Authorization: token } });
    }

    async getActiveLiveSession(courseId: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAM_COURSE_ACTIVE(courseId));
    }

    async getCourseExams(courseId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.COURSES_EXAMS(courseId), { headers: { Authorization: token } });
    }

    async getCourseExam(courseId: string, examId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.COURSES_EXAM_BY_ID(courseId, examId), { headers: { Authorization: token } });
    }

    async submitExam(courseId: string, examId: string, body: { answers: any[] }, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.COURSES_EXAM_SUBMIT(courseId, examId), body, { headers: { Authorization: token } });
    }

    async getMyExamResult(courseId: string, examId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.COURSES_EXAM_MY_RESULT(courseId, examId), { headers: { Authorization: token } });
    }

    async getMyCertificates(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.COURSES_MY_CERTIFICATES, { headers: { Authorization: token } });
    }

    async renderCertificate(uniqueCode: string, token: string) {
        return httpClient.get(`/courses/my-certificates/${uniqueCode}/render`, { headers: { Authorization: token } });
    }

    async retroactiveCertificate(courseId: string, token: string) {
        return httpClient.post(`/courses/${courseId}/retroactive-certificate`, {}, { headers: { Authorization: token } });
    }
}