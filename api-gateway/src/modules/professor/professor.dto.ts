import { IsInt, IsOptional, IsString, Min, IsBoolean, IsUUID, IsArray, ValidateNested, IsNumber } from 'class-validator';
import { Type } from 'class-transformer';

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
    @IsUUID()
    courseId: string;

    @IsString()
    title: string;

    @IsOptional()
    @IsString()
    description?: string;

    @IsOptional()
    @IsInt()
    @Min(1)
    durationMinutes?: number;

    @IsOptional()
    @IsInt()
    @Min(0)
    passingScore?: number;

    @IsOptional()
    @IsBoolean()
    isAutoGraded?: boolean;
}

export class UpdateExamDto {
    @IsOptional()
    @IsString()
    title?: string;

    @IsOptional()
    @IsString()
    description?: string;

    @IsOptional()
    @IsInt()
    @Min(1)
    durationMinutes?: number;

    @IsOptional()
    @IsInt()
    @Min(0)
    passingScore?: number;
}

export class QuestionOptionDto {
    @IsString()
    optionText: string;

    @IsBoolean()
    isCorrect: boolean;

    @IsOptional()
    @IsInt()
    order?: number;
}

export class CreateExamQuestionDto {
    @IsString()
    questionText: string;

    @IsOptional()
    @IsString()
    questionType?: string;

    @IsOptional()
    @IsNumber()
    points?: number;

    @IsOptional()
    @IsInt()
    order?: number;

    @IsOptional()
    @IsArray()
    @ValidateNested({ each: true })
    @Type(() => QuestionOptionDto)
    options?: QuestionOptionDto[];
}

export class SubmitAnswerDto {
    @IsUUID()
    questionId: string;

    @IsOptional()
    @IsUUID()
    selectedOptionId?: string;

    @IsOptional()
    @IsString()
    textAnswer?: string;
}

export class SubmitExamAttemptDto {
    @IsUUID()
    examId: string;

    @IsUUID()
    studentProfileId: string;

    @IsArray()
    @ValidateNested({ each: true })
    @Type(() => SubmitAnswerDto)
    answers: SubmitAnswerDto[];
}

export class GradeOpenTextDto {
    @IsUUID()
    attemptId: string;

    @IsUUID()
    questionId: string;

    @IsBoolean()
    isCorrect: boolean;

    @IsInt()
    @Min(0)
    pointsAwarded: number;

    @IsOptional()
    @IsString()
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
    @IsUUID()
    templateId: string;

    @IsUUID()
    studentProfileId: string;

    @IsUUID()
    courseId: string;
}
