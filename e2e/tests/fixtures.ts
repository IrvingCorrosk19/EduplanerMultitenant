/** IDs de escuela (DEV) — validados contra PostgreSQL. */
export const SCHOOL_CANTON = 'cc4e5e11-1be8-42de-8193-428f4484041c';
export const SCHOOL_SAN_MIGUELITO = '6e42399f-6f17-4585-b92e-fa4fff02cb65';

export const PASSWORD = 'Test#2026';

/**
 * IDs fijos opcionales (deben coincidir con `insert_e2e_roles_per_school.sql` si se usan UUID fijos).
 * Las pruebas de ownership resuelven el ID vía UI (`fetchSanMiguelitoStudentUserId`) o `E2E_SM_STUDENT_USER_ID`.
 */
export const USER_ID_ADMIN_OTHER_SCHOOL = 'b0b35595-cc47-4a3e-9233-1c57809daca5';
export const USER_ID_STUDENT_OTHER_SCHOOL = '2e3ed445-d285-4d7d-b262-5e8fcd3c3cec';
