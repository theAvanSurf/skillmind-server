import { Controller, Get, Post, Put, Param, Body, Headers, Query } from '@nestjs/common';
import { ApiBearerAuth, ApiTags } from '@nestjs/swagger';
import sanitizeHtml from 'sanitize-html';
import { ProfessorService } from './professor.service';
import {
    CreateProfessorProfileDto, UpdateProfessorProfileDto,
    CreateExamDto, UpdateExamDto, CreateExamQuestionDto,
    SubmitExamAttemptDto, GradeOpenTextDto,
    CreateCertificateTemplateDto, UpdateCertificateTemplateDto, ManualIssueCertificateDto,
} from './professor.dto';

@ApiTags('Professor')
@ApiBearerAuth()
@Controller('professor')
export class ProfessorController {
    constructor(private readonly professorService: ProfessorService) {}

    // ── Profile ───────────────────────────────────────────────────────────────
    @Post('profile')
    createProfile(@Body() dto: CreateProfessorProfileDto, @Headers('authorization') token: string) {
        return this.professorService.createProfile(dto, token);
    }

    @Get('profile')
    getMyProfile(@Headers('authorization') token: string) {
        return this.professorService.getMyProfile(token);
    }

    @Put('profile')
    updateProfile(@Body() dto: UpdateProfessorProfileDto, @Headers('authorization') token: string) {
        return this.professorService.updateProfile(dto, token);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    @Get('dashboard')
    getDashboard(@Headers('authorization') token: string) {
        return this.professorService.getDashboard(token);
    }

    @Get('earnings')
    getEarnings(@Headers('authorization') token: string) {
        return this.professorService.getEarnings(token);
    }

    @Get('students')
    getStudents(@Headers('authorization') token: string) {
        return this.professorService.getStudents(token);
    }

    // ── Stripe Connect ────────────────────────────────────────────────────────
    @Post('stripe/connect')
    createStripeConnect(@Query('returnUrl') returnUrl: string, @Headers('authorization') token: string) {
        return this.professorService.createStripeConnect(returnUrl, token);
    }

    @Get('stripe/status')
    getStripeStatus(@Headers('authorization') token: string) {
        return this.professorService.getStripeStatus(token);
    }

    // ── Exams ─────────────────────────────────────────────────────────────────
    @Post('exams')
    createExam(@Body() dto: CreateExamDto, @Headers('authorization') token: string) {
        return this.professorService.createExam(dto, token);
    }

    @Get('exams/:examId')
    getExam(@Param('examId') examId: string, @Headers('authorization') token: string) {
        return this.professorService.getExam(examId, token);
    }

    @Get('exams/course/:courseId')
    getExamsByCourse(@Param('courseId') courseId: string, @Headers('authorization') token: string) {
        return this.professorService.getExamsByCourse(courseId, token);
    }

    @Put('exams/:examId')
    updateExam(@Param('examId') examId: string, @Body() dto: UpdateExamDto, @Headers('authorization') token: string) {
        return this.professorService.updateExam(examId, dto, token);
    }

    @Post('exams/:examId/publish')
    publishExam(@Param('examId') examId: string, @Headers('authorization') token: string) {
        return this.professorService.publishExam(examId, token);
    }

    @Post('exams/:examId/questions')
    addQuestion(@Param('examId') examId: string, @Body() dto: CreateExamQuestionDto, @Headers('authorization') token: string) {
        return this.professorService.addQuestion(examId, dto, token);
    }

    @Get('exams/:examId/attempts')
    getAttempts(@Param('examId') examId: string, @Headers('authorization') token: string) {
        return this.professorService.getAttempts(examId, token);
    }

    @Post('exams/submit')
    submitAttempt(@Body() dto: SubmitExamAttemptDto, @Headers('authorization') token: string) {
        return this.professorService.submitAttempt(dto, token);
    }

    @Post('exams/attempts/grade')
    gradeOpenText(@Body() dto: GradeOpenTextDto, @Headers('authorization') token: string) {
        return this.professorService.gradeOpenText(dto, token);
    }

    // ── Courses ───────────────────────────────────────────────────────────────
    @Get('courses')
    getMyCourses(@Headers('authorization') token: string) {
        return this.professorService.getMyCourses(token);
    }

    @Post('courses')
    createCourse(@Body() dto: any, @Headers('authorization') token: string) {
        return this.professorService.createCourse(dto, token);
    }

    @Post('courses/:courseId/publish')
    publishCourse(@Param('courseId') courseId: string, @Headers('authorization') token: string) {
        return this.professorService.publishCourse(courseId, token);
    }

    // ── Certificates ──────────────────────────────────────────────────────────
    @Get('certificates/templates')
    getCertTemplates(@Headers('authorization') token: string) {
        return this.professorService.getCertificateTemplates(token);
    }

    @Post('certificates/templates')
    createCertTemplate(@Body() dto: CreateCertificateTemplateDto, @Headers('authorization') token: string) {
        if (dto.bodyHtml) {
            dto.bodyHtml = sanitizeHtml(dto.bodyHtml, {
                allowedTags: sanitizeHtml.defaults.allowedTags.concat(['h1', 'h2', 'img']),
                allowedAttributes: {
                    ...sanitizeHtml.defaults.allowedAttributes,
                    '*': ['style', 'class']
                }
            });
        }
        return this.professorService.createCertificateTemplate(dto, token);
    }

    @Put('certificates/templates/:templateId')
    updateCertTemplate(@Param('templateId') id: string, @Body() dto: UpdateCertificateTemplateDto, @Headers('authorization') token: string) {
        if (dto.bodyHtml) {
            dto.bodyHtml = sanitizeHtml(dto.bodyHtml, {
                allowedTags: sanitizeHtml.defaults.allowedTags.concat(['h1', 'h2', 'img']),
                allowedAttributes: {
                    ...sanitizeHtml.defaults.allowedAttributes,
                    '*': ['style', 'class']
                }
            });
        }
        return this.professorService.updateCertificateTemplate(id, dto, token);
    }

    @Post('certificates/issue')
    manualIssue(@Body() dto: ManualIssueCertificateDto, @Headers('authorization') token: string) {
        return this.professorService.manualIssueCertificate(dto, token);
    }

    @Get('certificates/course/:courseId')
    getCertsByCourse(@Param('courseId') courseId: string, @Headers('authorization') token: string) {
        return this.professorService.getCertsByCourse(courseId, token);
    }

    // ── Live Streams ──────────────────────────────────────────────────────────
    @Get('livestreams')
    getLiveStreams(@Headers('authorization') token: string) {
        return this.professorService.getLiveStreams(token);
    }

    @Get('livestreams/youtube-status')
    getYouTubeStatus(@Headers('authorization') token: string) {
        return this.professorService.getYouTubeStatus(token);
    }

    @Get('livestreams/oauth/url')
    getYouTubeOAuthUrl(@Headers('authorization') token: string) {
        return this.professorService.getYouTubeOAuthUrl(token);
    }

    @Post('livestreams/oauth/exchange')
    exchangeYouTubeCode(@Body() body: { code: string }, @Headers('authorization') token: string) {
        return this.professorService.exchangeYouTubeCode(body, token);
    }

    @Get('livestreams/course/:courseId/active')
    getActiveCourseSession(@Param('courseId') courseId: string, @Headers('authorization') token: string) {
        return this.professorService.getActiveCourseSession(courseId, token);
    }

    @Get('livestreams/:id/stream-key')
    getStreamKey(@Param('id') id: string, @Headers('authorization') token: string) {
        return this.professorService.getStreamKey(id, token);
    }

    @Get('livestreams/:id')
    getLiveStream(@Param('id') id: string, @Headers('authorization') token: string) {
        return this.professorService.getLiveStream(id, token);
    }

    @Post('livestreams')
    createLiveStream(@Body() body: any, @Headers('authorization') token: string) {
        return this.professorService.createLiveStream(body, token);
    }

    @Post('livestreams/:id/start')
    startLiveStream(@Param('id') id: string, @Headers('authorization') token: string) {
        return this.professorService.startLiveStream(id, token);
    }

    @Post('livestreams/:id/end')
    endLiveStream(@Param('id') id: string, @Headers('authorization') token: string) {
        return this.professorService.endLiveStream(id, token);
    }
}
