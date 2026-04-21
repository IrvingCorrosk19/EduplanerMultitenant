-- school_id aún NULL después de normalización (esperado solo en superadmin en users)
SELECT 'activity_types' AS tbl, COUNT(*) AS nulls FROM activity_types WHERE school_id IS NULL
UNION ALL SELECT 'activities', COUNT(*) FROM activities WHERE school_id IS NULL
UNION ALL SELECT 'area', COUNT(*) FROM area WHERE school_id IS NULL
UNION ALL SELECT 'attendance', COUNT(*) FROM attendance WHERE school_id IS NULL
UNION ALL SELECT 'audit_logs', COUNT(*) FROM audit_logs WHERE school_id IS NULL
UNION ALL SELECT 'discipline_reports', COUNT(*) FROM discipline_reports WHERE school_id IS NULL
UNION ALL SELECT 'email_jobs', COUNT(*) FROM email_jobs WHERE school_id IS NULL
UNION ALL SELECT 'grade_levels', COUNT(*) FROM grade_levels WHERE school_id IS NULL
UNION ALL SELECT 'groups', COUNT(*) FROM groups WHERE school_id IS NULL
UNION ALL SELECT 'messages', COUNT(*) FROM messages WHERE school_id IS NULL
UNION ALL SELECT 'orientation_reports', COUNT(*) FROM orientation_reports WHERE school_id IS NULL
UNION ALL SELECT 'security_settings', COUNT(*) FROM security_settings WHERE school_id IS NULL
UNION ALL SELECT 'specialties', COUNT(*) FROM specialties WHERE school_id IS NULL
UNION ALL SELECT 'student_activity_scores', COUNT(*) FROM student_activity_scores WHERE school_id IS NULL
UNION ALL SELECT 'students', COUNT(*) FROM students WHERE school_id IS NULL
UNION ALL SELECT 'subjects', COUNT(*) FROM subjects WHERE school_id IS NULL
UNION ALL SELECT 'teacher_work_plans', COUNT(*) FROM teacher_work_plans WHERE school_id IS NULL
UNION ALL SELECT 'trimester', COUNT(*) FROM trimester WHERE school_id IS NULL
UNION ALL SELECT 'users_non_super', COUNT(*) FROM users WHERE school_id IS NULL AND lower(trim(role)) <> 'superadmin'
UNION ALL SELECT 'users_superadmin', COUNT(*) FROM users WHERE school_id IS NULL AND lower(trim(role)) = 'superadmin'
UNION ALL SELECT 'subject_assignments', COUNT(*) FROM subject_assignments WHERE school_id IS NULL
ORDER BY nulls DESC, tbl;
