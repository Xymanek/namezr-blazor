# Namezr Product Requirements Document (PRD)

## Overview
Namezr is a feature-rich Blazor Web App for managing creators, supporters, questionnaires, polls, and eligibility workflows. It uses a feature-based structure, Havit.Blazor UI components, and Immediate.Apis for HTTP endpoints. The system supports both public and studio (admin/creator) user flows.

---

## Core Features

### 1. Creators
- **Creator Profiles:** Public and studio views for creator information.
- **Support Targets:** Manage external support platforms (Twitch, Patreon, YouTube, etc.).
- **Staff Management:** Invite and manage staff for creators.
- **Onboarding:** Includes Discord bot installation and onboarding flows.

### 2. Consumers (Supporters)
- **My Creators:** View and interact with creators a user supports.
- **Supporter Management:** Studio tools to view and manage supporters, their support status, and participation.

### 3. Questionnaires
- **Creation & Editing:** Studio users can create, edit, and version questionnaires.
- **Submission:** Public users can submit responses if eligible; eligibility is checked per questionnaire.
- **Approval Workflow:** Submissions can require approval, be auto-approved, or be closed.
- **Labels & Comments:** Submissions support labels and threaded comments (public and staff).
- **Submission Attributes:** Staff can add custom key-value metadata to submissions for internal tracking and organization.
- **Bulk & File Operations:** Download submission files individually or in bulk.
- **Selection Series:** Studio tools for managing selection rounds and batches.

### 4. Eligibility
- **Eligibility Configuration:** Define and manage eligibility rules for questionnaires and support plans.
- **Eligibility Presentation:** Public and studio components to display eligibility status and requirements.

### 5. Polls
- **Poll Creation:** Studio users can create and manage polls.
- **Voting:** Users can participate in polls if eligible.

### 6. Identity & Authentication
- **External Logins:** Support for Discord, Google, and other OAuth providers.
- **Profile Management:** Users can manage their profile and linked accounts.
- **Authorization:** Role-based access for creators, staff, and supporters.

### 7. Files
- **Upload/Download:** Secure upload and download endpoints for submission files.
- **Restrictions:** File type and size restrictions configurable per questionnaire.

### 8. Notifications
- **Email & Discord:** Notification dispatchers for email and Discord.
- **Submission Events:** Notify users and staff on submission/comment/approval events.

### 9. Studio (Admin) Tools
- **Dashboard:** Studio layouts and navigation for managing creators, questionnaires, supporters, and submissions.
- **Breadcrumbs & Navigation:** Consistent navigation using Havit.Blazor components.

---

## Endpoints (Immediate.Apis)
- **Questionnaires:** Create, update, submit, download files, bulk download, mutate labels, manage submission attributes.
- **Creators:** Onboarding, staff management, support target management.
- **Eligibility:** Eligibility checks and configuration.
- **Polls:** Create and manage polls.
- **Files:** Upload and download endpoints.
- **Identity:** External login, logout, profile management.
- **Selection Series:** Manage selection batches.

---

## User Flows
- **Public User:**
  - Browse creators and questionnaires.
  - Submit responses if eligible.
  - View own submissions and comments.
  - Manage profile and external logins.
- **Creator/Staff (Studio):**
  - Manage creator profile, staff, and support targets.
  - Create and manage questionnaires, submissions, and polls.
  - Review, approve, and comment on submissions.
  - Add custom attributes to submissions for internal tracking.
  - Manage eligibility and selection series.
  - View and manage supporters.

---

## Technology & Structure
- **Blazor Web App:** Static rendering for server-side, interactive features in `Namezr.Client` (WASM).
- **Havit.Blazor UI:** Preferred for all UI components.
- **Bootstrap 5.3:** Used for layout and helper classes.
- **Feature-based Structure:** Each feature (e.g., Questionnaires, Creators) is isolated in its own folder with pages, endpoints, data, and services.
- **Immediate.Apis:** All HTTP endpoints are implemented using Immediate.Apis.

---

## Third-Party Integrations
- **Discord:** Bot onboarding, login, and notifications.
- **Google:** OAuth login.
- **Twitch, Patreon, YouTube, BuyMeACoffee:** Support target integrations.

## Third-Party Service Interactions

Namezr integrates with several third-party services to enhance creator and supporter experiences. These integrations are primarily managed through support targets, authentication, and notifications. Key interactions include:

- **Discord**
  - OAuth login for user authentication.
  - Discord bot onboarding for creators, enabling automated notifications and community management features.
  - Dispatching notifications to Discord channels for submission events and other activities.
  - Requires `Server Members Intent` to be enabled in Discord settings.

- **Google**
  - OAuth login for user authentication.
  - Requires the `People API` to be enabled in Google Cloud settings.

- **Twitch, Patreon, YouTube, BuyMeACoffee**
  - Managed as support targets, allowing creators to link their external supporter platforms.
  - Enables tracking of supporter status and eligibility based on external subscriptions or memberships.
  - Home and join URLs are stored for each support target, and service-specific tokens may be used for API access (where supported).

- **Email (SMTP)**
  - Used for sending notifications to users and staff regarding submission events, approvals, and comments.
  - Configurable SMTP settings for outbound email delivery.

These integrations are designed to be extensible, allowing new platforms to be added with minimal changes to the core system. All sensitive credentials and tokens are managed via secure configuration and are not exposed to the client.

---

## Security & Access
- **Role-based Authorization:** Enforced at endpoint and UI level.
- **File Access:** Only authorized users can upload/download files.
- **Submission Access:** Only eligible users can submit; only staff can approve/review.

---

## Extensibility
- **Feature-based organization** allows for easy addition of new modules (e.g., new support platforms, new eligibility rules).
- **Immediate.Apis** enables rapid creation of new endpoints with consistent authorization and validation.

---

## Data Model: Creators, Support Targets, Supporters, and Users

- **Creators** represent individuals or organizations who run campaigns, questionnaires, or community activities on Namezr. Each creator has a profile and can have multiple staff members.

- **Support Targets** are external platforms (such as Twitch, Patreon, YouTube, BuyMeACoffee) linked to a creator. Each support target is associated with a single creator and represents a place where supporters can provide financial or community support. Support targets store metadata such as display name, logo, home/join URLs, and may include service-specific tokens for API access.

- **Supporters** are users who support a creator via one or more support targets. The system tracks which users are active supporters for each support target, enabling eligibility and participation features. Supporter status is determined by verifying external subscriptions or memberships on the linked platforms.

- **Users** are all registered participants in the system. A user can be a supporter, a creator, a staff member, or any combination. Users can manage their profile, participate in questionnaires, and interact with creators they support.

**Associations:**
- A creator can have multiple support targets.
- A support target belongs to one creator.
- A user can support multiple creators (via different support targets).
- The system maintains mappings between users and their active support targets to determine eligibility and access.
- Staff users are associated with creators and have management permissions.

This association model enables flexible community management, eligibility checks, and targeted communication between creators and their supporters.

---

### Mapping Between Supporters and Users

Supporters are a subset of users who have an active support relationship with a creator through one or more support targets. The mapping between supporters and users is established as follows:

- Each user is uniquely identified in the system through authentication (via external providers such as Discord or Google, or local accounts).
- When a user links or verifies their account with an external support platform (e.g., Twitch, Patreon), the system associates their Namezr user account with their external account(s).
- The system periodically or on-demand verifies the user's supporter status by checking their active subscriptions or memberships on the linked support targets.
- If a user is found to be an active supporter for a creator (based on external platform data), they are mapped as a supporter for that creator and support target within Namezr.
- This mapping is used to determine eligibility for participation in questionnaires, polls, and other creator-specific activities.

**Authentication Relationship:**
- Authentication ensures that each supporter is a verified user, preventing impersonation and enabling secure access to supporter-only features.
- OAuth logins (Discord, Google, etc.) are used to establish and verify user identity, which is then linked to external support platform accounts for supporter verification.

This approach ensures that only authenticated users with valid external support relationships are granted supporter status and access to exclusive creator content or actions.

---

## Notes
- All UI should use Havit.Blazor components where possible.
- Static rendering is enforced for all server-side code; interactive features must be in the WASM client.
- All endpoints are implemented using Immediate.Apis.

---

## Submission Attributes

Submission attributes provide a flexible way for staff to add custom metadata to questionnaire submissions for internal tracking, organization, and workflow management.

### Features
- **Key-Value Storage:** Each attribute consists of a string key (max 50 characters) and value (max 5000 characters).
- **Staff-Only Access:** Only creator staff can view, add, edit, or delete submission attributes.
- **Case-Insensitive Keys:** Attribute keys are case-insensitive with automatic whitespace trimming.
- **Unique Keys:** Each submission can have only one attribute per key (case-insensitive).
- **Real-Time Validation:** UI provides immediate feedback for duplicate keys and validation errors.
- **Contextual Tooltips:** Focus tooltips guide users about key behavior and validation rules.

### User Experience
- **Inline Editing:** Staff can add and edit attributes directly in the submission details view.
- **Readonly Keys:** Once saved, attribute keys become readonly to maintain data integrity.
- **Auto-Save:** Attributes are automatically saved when users blur input fields.
- **Visual Feedback:** Invalid states (duplicate keys, validation errors) are clearly indicated.

### Technical Implementation
- **Database:** Stored in `SubmissionAttributes` table with composite primary key (SubmissionId, Key).
- **API Endpoint:** `POST /api/submission-attributes/set` handles create/update/delete operations.
- **Validation:** FluentValidation ensures data integrity on both client and server.
- **Audit Trail:** All attribute changes are logged in submission history with full context.

### Audit & History
- **Complete Tracking:** All attribute creation, updates, and deletions are audited.
- **Change Context:** Audit logs include staff member, timestamps, keys, values, and previous values.
- **History Integration:** Attribute changes appear in submission history alongside other events.

### Use Cases
- **Internal Notes:** Add processing notes, priority levels, or status indicators.
- **Categorization:** Tag submissions with custom categories for filtering and reporting.
- **Workflow Tracking:** Record processing stages, assignments, or completion status.
- **Integration Data:** Store external system IDs or references for cross-platform workflows.

---

*Generated automatically from project structure and code analysis, June 25, 2025.*
