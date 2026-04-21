-- Pre-flight: rows where school_id is NULL or not a valid schools.id
SELECT 'academic_years' AS tbl, COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) AS to_fix FROM academic_years
UNION ALL SELECT 'activities', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM activities
UNION ALL SELECT 'activity_types', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM activity_types
UNION ALL SELECT 'area', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM area
UNION ALL SELECT 'attendance', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM attendance
UNION ALL SELECT 'audit_logs', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM audit_logs
UNION ALL SELECT 'counselor_assignments', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM counselor_assignments
UNION ALL SELECT 'discipline_reports', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM discipline_reports
UNION ALL SELECT 'email_configurations', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM email_configurations
UNION ALL SELECT 'email_jobs', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM email_jobs
UNION ALL SELECT 'grade_levels', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM grade_levels
UNION ALL SELECT 'groups', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM groups
UNION ALL SELECT 'id_card_template_fields', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM id_card_template_fields
UNION ALL SELECT 'messages', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM messages
UNION ALL SELECT 'orientation_reports', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM orientation_reports
UNION ALL SELECT 'payment_concepts', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM payment_concepts
UNION ALL SELECT 'payments', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM payments
UNION ALL SELECT 'prematriculation_periods', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM prematriculation_periods
UNION ALL SELECT 'prematriculations', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM prematriculations
UNION ALL SELECT 'school_id_card_settings', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM school_id_card_settings
UNION ALL SELECT 'school_schedule_configurations', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM school_schedule_configurations
UNION ALL SELECT 'security_settings', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM security_settings
UNION ALL SELECT 'shifts', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM shifts
UNION ALL SELECT 'specialties', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM specialties
UNION ALL SELECT 'student_activity_scores', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM student_activity_scores
UNION ALL SELECT 'student_payment_access', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM student_payment_access
UNION ALL SELECT 'students', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM students
UNION ALL SELECT 'subjects', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM subjects
UNION ALL SELECT 'teacher_work_plans', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM teacher_work_plans
UNION ALL SELECT 'time_slots', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM time_slots
UNION ALL SELECT 'trimester', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM trimester
UNION ALL SELECT 'subject_assignments', COUNT(*) FILTER (WHERE school_id IS NULL OR school_id NOT IN (SELECT id FROM schools)) FROM subject_assignments
ORDER BY to_fix DESC, tbl;
