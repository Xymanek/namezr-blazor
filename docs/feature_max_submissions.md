# Feature: Max Submissions per User

## Objective
Implement a feature that allows creators to specify the maximum number of submissions a user can make for a given eligibility option.

## Architectural Design:

1.  **Database Schema Update (`Namezr/Features/Eligibility/Data/EligibilityOptionEntity.cs`)**:
    *   Add a new integer property, `MaxSubmissionsPerUser`, to the `EligibilityOptionEntity`.
    *   The default value for this property will be `1`, meaning a single submission is allowed if not specified otherwise. A value of `0` could indicate unlimited submissions.
    *   This change will require generating a new Entity Framework Core migration.

2.  **API/Client Model Update (`Namezr.Client/Studio/Eligibility/Edit/EligibilityOptionEditModel.cs`)**:
    *   Add the `MaxSubmissionsPerUser` integer property to `EligibilityOptionEditModel`.
    *   The default value for this property will also be `1`.

3.  **Mapper Update**:
    *   Identify and update the relevant mapper (likely in `Namezr/Features/Eligibility/Mappers/`) that converts data between `EligibilityOptionEditModel` and `EligibilityOptionEntity` to include the new `MaxSubmissionsPerUser` property.

4.  **UI Update (`Namezr.Client/Studio/Eligibility/Edit/EligibilityOptionEditor.razor`)**:
    *   Add a UI control, such as a numeric input field, to `EligibilityOptionEditor.razor` to allow creators to configure the `MaxSubmissionsPerUser` property.

5.  **Backend Logic Update**:
    *   Modify the backend logic responsible for processing submissions (likely within `Namezr/Features/Questionnaires/Services/` or `Namezr/Features/Questionnaires/Endpoints/`) to check the `MaxSubmissionsPerUser` property and enforce the submission limit before accepting new submissions from users.

## Detailed Plan:

1.  **Modify `Namezr/Features/Eligibility/Data/EligibilityOptionEntity.cs`**:
    *   Add the following line:
        ```csharp
        public int MaxSubmissionsPerUser { get; set; } = 1;
        ```

2.  **Create Database Migration**:
    *   Execute a `dotnet ef migrations add AddMaxSubmissionsPerUserToEligibilityOption` command to generate the migration for this schema change.

3.  **Modify `Namezr.Client/Studio/Eligibility/Edit/EligibilityOptionEditModel.cs`**:
    *   Add the following line:
        ```csharp
        public int MaxSubmissionsPerUser { get; set; } = 1;
        ```

4.  **Identify and Modify Mapper**:
    *   Use `search_files` to locate the mapper file(s) responsible for converting between `EligibilityOptionEditModel` and `EligibilityOptionEntity`.
    *   Update the mapper logic to correctly map the `MaxSubmissionsPerUser` property in both directions.

5.  **Modify `Namezr.Client/Studio/Eligibility/Edit/EligibilityOptionEditor.razor`**:
    *   Add a numeric input field (e.g., `<HxInputNumber @bind-Value="EditModel.MaxSubmissionsPerUser" Label="Max Submissions Per User" />`) to allow setting the value.

6.  **Identify and Modify Submission Logic**:
    *   Locate the relevant backend files that handle submission processing (e.g., `Namezr/Features/Questionnaires/Endpoints/QuestionnaireSubmissionEndpoints.cs` or `Namezr/Features/Questionnaires/Services/QuestionnaireSubmissionService.cs`).
    *   Implement logic to:
        *   Retrieve the `MaxSubmissionsPerUser` value for the relevant `EligibilityOptionEntity`.
        *   Count existing submissions for the current user and questionnaire.
        *   If `MaxSubmissionsPerUser` is greater than 0 and the current submission count meets or exceeds this value, reject the new submission.

## Diagram:

```mermaid
graph TD
    A[User Request: Max Submissions per User] --> B{Architectural Design};

    B --> C[Database Schema: EligibilityOptionEntity.cs];
    C --> C1[Add MaxSubmissionsPerUser: int (default 1)];
    C1 --> C2[Generate EF Migration];

    B --> D[API/Client Model: EligibilityOptionEditModel.cs];
    D --> D1[Add MaxSubmissionsPerUser: int (default 1)];

    B --> E[Mapper Layer];
    E --> E1[Update Mapper to handle new property];

    B --> F[UI Layer: EligibilityOptionEditor.razor];
    F --> F1[Add Numeric Input for MaxSubmissionsPerUser];

    B --> G[Backend Logic: Submission Processing];
    G --> G1[Check MaxSubmissionsPerUser and enforce limit];

    C2 --> H[Apply Migration];
    H --> I[Updated Database];