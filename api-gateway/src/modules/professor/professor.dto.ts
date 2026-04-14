import { IsInt, IsOptional, IsString, Min, IsBoolean } from 'class-validator';

// ─── Professor Profile ────────────────────────────────────────────────────────
export class CreateProfessorProfileDto {
    @IsString()
    userId: string;

    @IsString()
    bio: string;

    @IsOptional()
    @IsString()
    expertise?: string;

    @IsOptional()
    @IsInt()
    @Min(0)
    yearsOfExperience?: number;

    @IsOptional()
    @IsString()
    linkedInUrl?: string;

    @IsOptional()
    @IsString()
    profilePhotoUrl?: string;
}

export class UpdateProfessorProfileDto {
    @IsOptional()
    @IsString()
    bio?: string;

    @IsOptional()
    @IsString()
    expertise?: string;

    @IsOptional()
    @IsInt()
    @Min(0)
    yearsOfExperience?: number;

    @IsOptional()
    @IsString()
    linkedInUrl?: string;

    @IsOptional()
    @IsString()
    profilePhotoUrl?: string;
}

// ─── Exam ─────────────────────────────────────────────────────────────────────
export class CreateExamDto {
    courseId: string;
    title: string;
    description?: string;
    durationMinutes?: number;
    passingScore?: number;
    isAutoGraded?: boolean;
}

export class UpdateExamDto {
    title?: string;
    description?: string;
    durationMinutes?: number;
    passingScore?: number;
}

export class CreateExamQuestionDto {
    questionText: string;
    questionType?: string;
    points?: number;
    order?: number;
    options?: { optionText: string; isCorrect: boolean; order: number }[];
}

export class SubmitExamAttemptDto {
    examId: string;
    studentProfileId: string;
    answers: { questionId: string; selectedOptionId?: string; textAnswer?: string }[];
}

export class GradeOpenTextDto {
    attemptId: string;
    questionId: string;
    isCorrect: boolean;
    pointsAwarded: number;
    feedback?: string;
}

// ─── Certificate ──────────────────────────────────────────────────────────────
export class CreateCertificateTemplateDto {
    @IsOptional()
    courseId?: string;

    @IsString()
    title: string;

    @IsOptional()
    @IsString()
    description?: string;

    @IsOptional()
    @IsString()
    bodyHtml?: string;

    @IsOptional()
    @IsBoolean()
    isDefault?: boolean;

    @IsOptional()
    @IsString()
    signatureUrl?: string;

    @IsOptional()
    @IsString()
    logoUrl?: string;

    @IsOptional()
    completionThresholdPercent?: number;
}

export class UpdateCertificateTemplateDto {
    @IsOptional()
    @IsString()
    title?: string;

    @IsOptional()
    @IsString()
    description?: string;

    @IsOptional()
    @IsString()
    bodyHtml?: string;

    @IsOptional()
    @IsBoolean()
    isDefault?: boolean;

    @IsOptional()
    @IsString()
    signatureUrl?: string;

    @IsOptional()
    @IsString()
    logoUrl?: string;

    @IsOptional()
    completionThresholdPercent?: number;
}

export class ManualIssueCertificateDto {
    templateId: string;
    studentProfileId: string;
    courseId: string;
}
