# Professor Features — Implementation Plan & Progress

## Overview
This document outlines the complete plan to implement the Professor features, divided into 10 Sprints.

### Sprint 1 — Domain Entities + Persistence Layer (Almost Complete ✅)
**Backend:**
- **Enums:** Created `ExamStatus`, `LiveSessionStatus`, `PayoutStatus`, `ExamQuestionType`, `YouTubeStreamVisibility`.
- **Entities:** Modified `Course.cs` to add `ProfessorId`. Created `ProfessorProfile`, `Enrollment`, `CertificateTemplate`, `Certificate`, `Exam`, `ExamQuestion`, `QuestionOption`, `ExamAttempt`, `AttemptAnswer`, and `LiveSession`.
- **Interfaces:** Created domain interfaces for repositories (`IProfessorRepository`, `IExamRepository`, `ICertificateRepository`, `ILiveSessionRepository`).
- **Entity Configurations:** Added EF Core configuration mappings for all new entities.
- **DbContext:** Added `DbSet`s and applied configurations in `SkillMindDbContext`.
- **Repositories:** Created `ProfessorRepository`, `ExamRepository`, `CertificateRepository`, and `LiveSessionRepository` and injected them into DI container via `ServicesInjection.cs`.
- **🛑 NEXT STEP (CONTINUE HERE TOMORROW):** Generate the EF Core Migration. You need to run `dotnet ef migrations add ProfessorFeatures --project SkillMind.Infrastructure.Persistence --startup-project SkillMind.WebAPI` and update the database.

### Sprint 2 — Professor Registration
**Backend:**
- Endpoint for setting up `ProfessorProfile` (bio, expertise, etc.).
- Stripe Connect account creation at registration.
**Frontend:**
- Add role selection step in the auth flow (Student vs Professor).
- `ProfessorInfoStep` form (bio, expertise, years of experience, LinkedIn).
- `StripeConnectStep` form (connect button or skip for later).
- Store update for registration flow.

### Sprint 3 — Professor Dashboard Backend
**Backend:**
- Implement `ProfessorRepository`.
- Create `IProfessorService` and `ProfessorService` (dashboard metrics, earnings aggregation).
- Create `ProfessorController` (endpoints for dashboard stats, courses, earnings, students).

### Sprint 4 — Professor Dashboard Frontend
**Frontend:**
- Build `/professor/dashboard` route.
- `DashboardSummaryCards` component (total students, earnings, etc.).
- `EarningsChart` (Recharts) and `EngagementChart` (metrics charts).
- `AlertsPanel` for pending reviews/exams.
- Role guards for `/professor/*` routes.

### Sprint 5 — Course/Season/Lesson Management
**Backend:**
- Update `CreateCourse` to use `ProfessorId` from JWT.
- CRUD operations for Season and Lesson (re-check DB persistence).
- Cloudinary signed upload endpoint for videos.
**Frontend:**
- `/professor/courses` list page.
- `CreateCoursePage` and `LessonManagerPage` (drag-and-drop reordering with `@dnd-kit`).
- Cloudinary video upload UI integration.

### Sprint 6 — Exams (Builder + Auto-grade + Manual grade)
**Backend:**
- `IExamService` & `ExamService` (CRUD, publish, generic grading).
- Auto-grading logic for Multiple Choice / True-False.
- `ExamsController`.
**Frontend:**
- `ExamBuilderPage` for professors.
- Exam-taking UI for students.
- Professor grade review UI.

### Sprint 7 — Certificates (Templates + Auto-issuance)
**Backend:**
- `ICertificateService` & `CertificateService`.
- Auto-issue hook on course progress update (trigger-based).
- `CertificatesController`.
**Frontend:**
- `CertificateTemplateEditor` page.
- Issued certificates list page.

### Sprint 8 — YouTube Live Streaming
**Backend:**
- `ILiveStreamService` & `LiveStreamService` integrating YouTube Data API v3.
- OAuth 2.0 flow (authorize & callback) handling. Redirect URI: `https://skillmind-api.avansurf.com/api/v1/livestreams/oauth/callback`.
- `LiveStreamsController` for create, start, and stop.
**Frontend:**
- `/professor/live` management page and `CreateStreamPage`.
- Embedded YouTube player on student course view.

### Sprint 9 — Earnings + Stripe Connect Payouts
**Backend:**
- Stripe Connect webhook handler.
- Payout status synchronization.
**Frontend:**
- `/professor/earnings` page to view and manage Stripe account access.

### Sprint 10 — Polish (Notifications, Responsive, Error Handling)
**Backend:**
- Kafka events to notify students (exam graded, cert issued).
**Frontend:**
- Mobile responsiveness on all professor pages.
- Standardized error states and retry logic.

---

## 🤖 AGENT HANDOFF CONTEXT — READ THIS BEFORE CONTINUING

This section contains highly specific architectural context and decisions so that *any* agent can seamlessly pick up this process. 

### 1. Architectural Foundation & Decisions
*   **Design Pattern**: The backend (`skillmind-server`) uses Clean Architecture (`Core.Domain`, `Core.Application`, `Infrastructure.Identity`, `Infrastructure.Persistence`, `WebAPI`). Ensure all logic strictly respects these layers (e.g., Controllers only talk to interfaces in `Core.Application`, Services implement `Core.Application` and talk to `Core.Domain.Interfaces` implemented by `Infrastructure.Persistence`).
*   **Authentication & Roles**:
    *   The `Roles` enum in `SkillMind.Core.Domain.Enums` already contains `Professor = 0`. Identity is seeded with this role.
    *   **CRITICAL**: `ProfessorProfile` domain entity links to ASP.NET Identity `ApplicationUser` using `UserId` (string).
*   **Decisions Made with User**:
    *   **Videos**: We are using **Cloudinary** for video uploads, not S3. Professors will upload videos directly to Cloudinary and the resulting URLs are stored in `Lesson.VideoUrl`.
    *   **Payouts**: We are using **Stripe Connect**. We ask the professor to set it up during the registration wizard, but they can click "Skip for Later".
    *   **Certificates**: Certificates are auto-issued on course progress updates in real-time, **not** via background CRON jobs. When progress reaches `CertificateTemplate.CompletionThresholdPercent`, issue it.
    *   **Live Sessions**: We are using the **YouTube Data API v3**. The required OAuth 2.0 redirect URI is `https://skillmind-api.avansurf.com/api/v1/livestreams/oauth/callback`. OAuth tokens are stored in the `ProfessorProfile` entity.

### 2. The Multi-Step Registration Wizard (Specifics)
*   **Frontend Location**: `skillmind-client/src/features/auth/forms/`. 
*   **State Management**: Uses Zustand (`create-user-storage.ts`).
*   **Flow Changes (To Implement)**:
    1.  In `Step2Form.tsx` (Account Info), add a UI toggle to select `Role` (Student vs. Professor). Update Zustand `draft.role`.
    2.  If the role is Professor, **skip Step 4** (`Step4Form.tsx` - Plan selection), as professors do not pay subscription plans.
    3.  Create a branching step: `ProfessorInfoStep.tsx` (to capture `Bio`, `Expertise`, `YearsOfExperience`, `LinkedInUrl`).
    4.  Create another branching step: `StripeConnectStep.tsx` (a UI page to either authenticate with Stripe Connect or skip).
*   **Backend Handoff**: Once the final registration is submitted via `authServices.signUp`, the backend `AccountServices.RegisterUser` creates the Identity user with `Role = 0`. Right after email verification and token retrieval, the frontend must hit a new endpoint `POST /api/v1/professor/profile` to create the `ProfessorProfile` domain entity with the extra professor data, because `AuthServices` (which creates the Identity user) lives in `Infrastructure.Identity`, while `ProfessorProfile` lives in `Core.Domain`. Keep them decoupled!

### 3. Backend Specifics (What's Done & What's Next)
*   **Completed**: All EF models (`Exam`, `Certificate`, `Enrollment`, `ProfessorProfile`, etc.) are written. The `SkillMindDbContext` in `Infrastructure.Persistence` is updated. Repositories are written and added to `ServicesInjection.cs`.
*   **Next Step Immediately**:
    1. Open terminal and run: `dotnet ef migrations add ProfessorFeatures --project SkillMind.Infrastructure.Persistence --startup-project SkillMind.WebAPI`
    2. Then run: `dotnet ef database update --project SkillMind.Infrastructure.Persistence --startup-project SkillMind.WebAPI`
*   **Application Layer Services**: Once Sprint 1 db migrations run, you must build the `IProfessorService`, `IExamService`, etc., inside `SkillMind.Core.Application`, along with their DTOs inside `Core/SkillmindCore/SkillMind.Core.Application/Dtos/`.

### 4. Frontend Professor Dashboard Architecture
*   **Routing**: Place all professor routes inside `src/app/(authenticated)/professor/*`.
*   **Components**: Store professor-specific components inside `src/features/professor/components/`. 
*   **Data Fetching**: The Next.js frontend uses a custom service class pattern (`auth-services.ts`, `courses-services.ts`). You need to create `professor-services.ts` to interface with the new backend API routes. Use Recharts for the Earnings/Engagement charts.
*   **Styling**: The platform heavily uses Tailwind CSS with dark gradients/glassmorphism (refer to `AuthLayout.tsx` for visual cues). Ensure professor dashboards look premium with micro-animations.

### 5. Cross-Service & Infrastructure Integration
*   **API Gateway (NestJS)**: Located in `skillmind-server/api-gateway`. Update `src/app.module.ts` or relevant proxy config to route `/api/v1/professor`, `/api/v1/exams`, `/api/v1/certificates`, `/api/v1/livestreams` to the core WebAPI.
*   **Notification Service (NestJS)**: Located in `skillmind-server/notification-service`. It consumes Kafka. Define topics:
    *   `student.enrollment.created` (Notifier for professor)
    *   `student.exam.submitted` (Notifier for professor)
    *   `professor.exam.graded` (Notifier for student)
    *   `student.certificate.issued` (Notifier for student)
*   **Environment Variables**: Add `CLOUDINARY_URL`, `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`, and `GOOGLE_OAUTH_CLIENT_ID` to the `docker-compose.yml` or relevant `.env` files.
*   **Database**: PostgreSQL runs in a container. Persistence is handled via EF Core Migrations.

### 6. Key Files Map (Sprint 1 Outputs)
*   **Entities**: `skillmind-server/Core/SkillmindCore/SkillMind.Core.Domain/Entities/`
*   **Enums**: `skillmind-server/Core/SkillmindCore/SkillMind.Core.Domain/Enums/`
*   **Repositories**: `skillmind-server/Core/SkillmindCore/SkillMind.Infrastructure.Persistence/Repositories/`
*   **Configurations**: `skillmind-server/Core/SkillmindCore/SkillMind.Infrastructure.Persistence/EntitiesConfigurations/`
*   **DbContext**: `skillmind-server/Core/SkillmindCore/SkillMind.Infrastructure.Persistence/Context/SkillMindDbContext.cs`
*   **DI Registration**: `skillmind-server/Core/SkillmindCore/SkillMind.Infrastructure.Persistence/ServicesInjection.cs`

### 7. Frontend Visual & Technical Standards
*   **UI Components**: Use the existing library in `skillmind-client/src/components/ui`.
*   **Consistency**: The "Professor Dashboard" must mirror the sleek, fluid, dark mode aesthetic of the SkillMind platform. Use Recharts for metrics.
*   **Auth Guard**: Implement high-order component (HOC) or middleware to restrict `/professor/*` routes to users with `Role === 0`.

### 8. Security & Operations (Hardening the Platform)
*   **Data Protection**: Use ASP.NET Core Data Protection (`IDataProtector`) to encrypt `YouTubeAccessToken` and `YouTubeRefreshToken` before saving to the database. These are sensitive credentials.
*   **Stripe Webhooks**: Ensure the `PaymentController` in the backend handles:
    *   `account.updated`: To sync `PayoutStatus` in `ProfessorProfile`.
    *   `checkout.session.completed`: If implementing single course purchases.
*   **Cloudinary Presets**: Create an "unsigned" upload preset in Cloudinary for lessons to allow the frontend `CldUploadWidget` to upload directly without a backend signature for better performance (if preferred), or use "signed" for max security.
*   **Kafka Dead Letter Queue**: Ensure the notification service handles retry logic for failed email delivery (SMTP) to prevent event loss.

**End of Exhaustive Handoff Context.**

---

## 🛠 DEEP DIVE TECHNICAL SPECIFICATIONS

### 1. Sprint 2: Professor Profile Setup DTO
The `POST /api/v1/professor/profile` request should mirror:
```json
{
  "userId": "guid-string",
  "bio": "Expert in software architecture...",
  "expertise": "C#, React, System Design",
  "yearsOfExperience": 12,
  "linkedInUrl": "https://linkedin.com/in/...",
  "profilePhotoUrl": "cloudinary-url"
}
```

### 2. Sprint 3: Dashboard Aggregation Logic
The `ProfessorRepository.GetDashboardSummaryAsync` should return:
- `TotalEarnings`: Sum of `PaidAmount` from `Enrollments` filtered by `ProfessorId`.
- `ActiveStudents`: Count of distinct `ProfileId` in `CourseProgress` updated within the last 30 days.
- `CourseCompletionRate`: Average `ProgressPercent` across all enrollments.

### 3. Sprint 6: Exam Auto-Grading Logic
In `ExamService.GradeAttemptAsync`:
1.  Fetch `Attempt` with `Answers` and `Question.Options`.
2.  Iterate `Answers`:
    -   IF `QuestionType == MultipleChoice`: Compare `SelectedOptionId` with `Option.IsCorrect`.
    -   IF `QuestionType == OpenText`: Mark `IsCorrect = null` (pending review).
3.  Calculate `Score` based on `Points`.

### 4. Sprint 8: YouTube OAuth Scopes
- `openid`, `email`
- `https://www.googleapis.com/auth/youtube`
- `https://www.googleapis.com/auth/youtube.force-ssl`

---

## 🏔 PROJECT PHILOSOPHY & Persona Map

### 1. Rationale
Decoupled Identity vs Profile ensures that an Auth failure doesn't wipe Domain data, and allows for scalability. 

### 2. Persona Map
- **Day 1**: Fast verification, fast payout setup.
- **Day 5**: "Where are my students? Is my content good?" (Needs analytics).
- **Day 30**: "I want to reward my top students" (Needs certificates).

---

## 🛠 TROUBLESHOOTING GUIDE

- **EF Migration**: Ensure `--startup-project SkillMind.WebAPI` is used.
- **YouTube Redirect**: Hostname must be absolute and SSL-enabled in Google Console.
- **Kafka**: Check if Zookeeper and Kafka containers are healthy before starting core services.

**ULTIMATE END OF DOCUMENT.**
