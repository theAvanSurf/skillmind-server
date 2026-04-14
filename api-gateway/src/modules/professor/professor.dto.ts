// ─── Professor Profile ────────────────────────────────────────────────────────
export class CreateProfessorProfileDto {
    userId: string;
    bio: string;
    expertise?: string;
    yearsOfExperience?: number;
    linkedInUrl?: string;
    profilePhotoUrl?: string;
}

export class UpdateProfessorProfileDto {
    bio?: string;
    expertise?: string;
    yearsOfExperience?: number;
    linkedInUrl?: string;
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
    courseId: string;
    title: string;
    description?: string;
    signatureUrl?: string;
    logoUrl?: string;
    completionThresholdPercent?: number;
}

export class UpdateCertificateTemplateDto {
    title?: string;
    description?: string;
    signatureUrl?: string;
    logoUrl?: string;
    completionThresholdPercent?: number;
}

export class ManualIssueCertificateDto {
    templateId: string;
    studentProfileId: string;
    courseId: string;
}
