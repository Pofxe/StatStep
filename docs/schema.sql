-- Stepik Analytics Database Schema
-- PostgreSQL DDL

-- Пользователи системы
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(200) NOT NULL,
    email VARCHAR(200),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP WITH TIME ZONE
);

-- Курсы Stepik
CREATE TABLE IF NOT EXISTS courses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stepik_course_id INTEGER UNIQUE NOT NULL,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    cover_url VARCHAR(1000),
    is_enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_synced_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX idx_courses_stepik_id ON courses(stepik_course_id);
CREATE INDEX idx_courses_is_enabled ON courses(is_enabled);

-- История синхронизаций
CREATE TABLE IF NOT EXISTS sync_runs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    started_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    finished_at TIMESTAMP WITH TIME ZONE,
    status VARCHAR(20) NOT NULL DEFAULT 'running', -- running, ok, failed
    error_text TEXT,
    fetched_items_count INTEGER DEFAULT 0
);

CREATE INDEX idx_sync_runs_course_started ON sync_runs(course_id, started_at DESC);

-- Ежедневные метрики
CREATE TABLE IF NOT EXISTS metrics_daily (
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    date DATE NOT NULL,
    
    -- Решения задач
    total_submissions INTEGER DEFAULT 0,
    correct_submissions INTEGER DEFAULT 0,
    wrong_submissions INTEGER DEFAULT 0,
    
    -- Ученики
    new_learners INTEGER DEFAULT 0,
    certificates INTEGER DEFAULT 0,
    
    -- Репутация и знания (заглушка - Stepik API не предоставляет)
    reputation_delta INTEGER DEFAULT 0,
    knowledge_delta INTEGER DEFAULT 0,
    
    -- Отзывы и рейтинг
    reviews_count INTEGER DEFAULT 0,
    rating_value DOUBLE PRECISION DEFAULT 0,
    rating_delta DOUBLE PRECISION DEFAULT 0,
    reviews_avg DOUBLE PRECISION DEFAULT 0,
    
    -- Активность
    active_learners_dau INTEGER DEFAULT 0,
    
    PRIMARY KEY (course_id, date)
);

CREATE INDEX idx_metrics_daily_date ON metrics_daily(date);
CREATE INDEX idx_metrics_daily_course_date ON metrics_daily(course_id, date DESC);

-- Опциональная таблица почасовых метрик (для режима "день")
CREATE TABLE IF NOT EXISTS metrics_hourly (
    course_id UUID NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
    datetime TIMESTAMP WITH TIME ZONE NOT NULL,
    
    total_submissions INTEGER DEFAULT 0,
    correct_submissions INTEGER DEFAULT 0,
    wrong_submissions INTEGER DEFAULT 0,
    active_learners INTEGER DEFAULT 0,
    
    PRIMARY KEY (course_id, datetime)
);

CREATE INDEX idx_metrics_hourly_course_datetime ON metrics_hourly(course_id, datetime DESC);

-- Вспомогательная функция для UPSERT метрик
CREATE OR REPLACE FUNCTION upsert_daily_metrics(
    p_course_id UUID,
    p_date DATE,
    p_total_submissions INTEGER,
    p_correct_submissions INTEGER,
    p_wrong_submissions INTEGER,
    p_new_learners INTEGER,
    p_active_learners_dau INTEGER,
    p_reviews_count INTEGER,
    p_rating_value DOUBLE PRECISION
) RETURNS VOID AS $$
BEGIN
    INSERT INTO metrics_daily (
        course_id, date, total_submissions, correct_submissions, wrong_submissions,
        new_learners, active_learners_dau, reviews_count, rating_value
    ) VALUES (
        p_course_id, p_date, p_total_submissions, p_correct_submissions, p_wrong_submissions,
        p_new_learners, p_active_learners_dau, p_reviews_count, p_rating_value
    )
    ON CONFLICT (course_id, date) DO UPDATE SET
        total_submissions = EXCLUDED.total_submissions,
        correct_submissions = EXCLUDED.correct_submissions,
        wrong_submissions = EXCLUDED.wrong_submissions,
        new_learners = EXCLUDED.new_learners,
        active_learners_dau = EXCLUDED.active_learners_dau,
        reviews_count = EXCLUDED.reviews_count,
        rating_value = EXCLUDED.rating_value;
END;
$$ LANGUAGE plpgsql;

-- Комментарии к таблицам
COMMENT ON TABLE users IS 'Пользователи системы Stepik Analytics';
COMMENT ON TABLE courses IS 'Отслеживаемые курсы Stepik';
COMMENT ON TABLE sync_runs IS 'История синхронизаций с Stepik API';
COMMENT ON TABLE metrics_daily IS 'Агрегированные метрики по дням';
COMMENT ON TABLE metrics_hourly IS 'Детальные метрики по часам (опционально)';

-- Hangfire требует свои таблицы, они создаются автоматически при запуске
