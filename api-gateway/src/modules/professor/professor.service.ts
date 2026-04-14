import { Injectable } from '@nestjs/common';
import httpClient from '../../config/baseHttpClient';
import { API_ENDPOINTS } from '../../common/endpoints';
import {
    CreateProfessorProfileDto, UpdateProfessorProfileDto,
    CreateExamDto, UpdateExamDto, CreateExamQuestionDto,
    SubmitExamAttemptDto, GradeOpenTextDto,
    CreateCertificateTemplateDto, UpdateCertificateTemplateDto, ManualIssueCertificateDto,
} from './professor.dto';

@Injectable()
export class ProfessorService {
    // ── Profile ───────────────────────────────────────────────────────────────
    createProfile(body: CreateProfessorProfileDto, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_PROFILE, body, { headers: { Authorization: token } });
    }
    getMyProfile(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_PROFILE, { headers: { Authorization: token } });
    }
    updateProfile(body: UpdateProfessorProfileDto, token: string) {
        return httpClient.put(API_ENDPOINTS.CORE.PROFESSOR_PROFILE, body, { headers: { Authorization: token } });
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    getDashboard(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_DASHBOARD, { headers: { Authorization: token } });
    }
    getEarnings(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_EARNINGS, { headers: { Authorization: token } });
    }
    getStudents(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_STUDENTS, { headers: { Authorization: token } });
    }

    // ── Stripe ────────────────────────────────────────────────────────────────
    createStripeConnect(returnUrl: string, token: string) {
        return httpClient.post(`${API_ENDPOINTS.CORE.PROFESSOR_STRIPE_CONNECT}?returnUrl=${encodeURIComponent(returnUrl)}`, {}, { headers: { Authorization: token } });
    }
    getStripeStatus(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_STRIPE_STATUS, { headers: { Authorization: token } });
    }

    // ── Exams ─────────────────────────────────────────────────────────────────
    createExam(body: CreateExamDto, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_EXAMS, body, { headers: { Authorization: token } });
    }
    getExam(examId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_EXAM_BY_ID(examId), { headers: { Authorization: token } });
    }
    getExamsByCourse(courseId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_EXAMS_BY_COURSE(courseId), { headers: { Authorization: token } });
    }
    updateExam(examId: string, body: UpdateExamDto, token: string) {
        return httpClient.put(API_ENDPOINTS.CORE.PROFESSOR_EXAM_BY_ID(examId), body, { headers: { Authorization: token } });
    }
    publishExam(examId: string, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_EXAM_PUBLISH(examId), {}, { headers: { Authorization: token } });
    }
    addQuestion(examId: string, body: CreateExamQuestionDto, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_EXAM_QUESTIONS(examId), body, { headers: { Authorization: token } });
    }
    getAttempts(examId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_EXAM_ATTEMPTS(examId), { headers: { Authorization: token } });
    }
    submitAttempt(body: SubmitExamAttemptDto, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_EXAMS_SUBMIT, body, { headers: { Authorization: token } });
    }
    gradeOpenText(body: GradeOpenTextDto, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_EXAMS_GRADE, body, { headers: { Authorization: token } });
    }

    // ── Courses ───────────────────────────────────────────────────────────────
    getMyCourses(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_COURSES, { headers: { Authorization: token } });
    }
    createCourse(body: any, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_COURSES, body, { headers: { Authorization: token } });
    }
    publishCourse(courseId: string, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_COURSE_PUBLISH(courseId), {}, { headers: { Authorization: token } });
    }

    // ── Certificate Templates ─────────────────────────────────────────────────
    getCertificateTemplates(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_CERT_TEMPLATES, { headers: { Authorization: token } });
    }
    createCertificateTemplate(body: CreateCertificateTemplateDto, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_CERT_TEMPLATES, body, { headers: { Authorization: token } });
    }
    updateCertificateTemplate(templateId: string, body: UpdateCertificateTemplateDto, token: string) {
        return httpClient.put(API_ENDPOINTS.CORE.PROFESSOR_CERT_TEMPLATE_BY_ID(templateId), body, { headers: { Authorization: token } });
    }
    manualIssueCertificate(body: ManualIssueCertificateDto, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_CERT_ISSUE, body, { headers: { Authorization: token } });
    }
    getCertsByCourse(courseId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_CERTS_BY_COURSE(courseId), { headers: { Authorization: token } });
    }

    // ── Live Streams ──────────────────────────────────────────────────────────
    getLiveStreams(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAMS, { headers: { Authorization: token } });
    }
    getLiveStream(id: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAM_BY_ID(id), { headers: { Authorization: token } });
    }
    createLiveStream(body: any, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAMS, body, { headers: { Authorization: token } });
    }
    startLiveStream(id: string, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAM_START(id), {}, { headers: { Authorization: token } });
    }
    endLiveStream(id: string, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAM_END(id), {}, { headers: { Authorization: token } });
    }
    getYouTubeOAuthUrl(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAM_OAUTH_URL, { headers: { Authorization: token } });
    }
    exchangeYouTubeCode(body: { code: string }, token: string) {
        return httpClient.post(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAM_OAUTH_EXCHANGE, body, { headers: { Authorization: token } });
    }
    getYouTubeStatus(token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAM_YOUTUBE_STATUS, { headers: { Authorization: token } });
    }
    getActiveCourseSession(courseId: string, token: string) {
        return httpClient.get(API_ENDPOINTS.CORE.PROFESSOR_LIVESTREAM_COURSE_ACTIVE(courseId), { headers: { Authorization: token } });
    }
    getStreamKey(sessionId: string, token: string) {
        return httpClient.get(`/live-streams/${sessionId}/stream-key`, { headers: { Authorization: token } });
    }
}
