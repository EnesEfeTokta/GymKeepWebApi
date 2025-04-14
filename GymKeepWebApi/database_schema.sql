-- Users tablosu
CREATE TABLE IF NOT EXISTS users (
    user_id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Exercises tablosu
CREATE TABLE IF NOT EXISTS exercises (
    exercise_id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(user_id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    reps_or_duration INTEGER NOT NULL,
    category VARCHAR(50) NOT NULL,
    exercise_date DATE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Regional Workouts tablosu
CREATE TABLE IF NOT EXISTS regional_workouts (
    workout_id SERIAL PRIMARY KEY,
    region VARCHAR(50) NOT NULL UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Regional Workout Exercises tablosu
CREATE TABLE IF NOT EXISTS regional_workout_exercises (
    workout_id INTEGER REFERENCES regional_workouts(workout_id) ON DELETE CASCADE,
    exercise_name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (workout_id, exercise_name)
);

-- Calorie Calculations tablosu
CREATE TABLE IF NOT EXISTS calorie_calculations (
    calculation_id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(user_id) ON DELETE CASCADE,
    age INTEGER NOT NULL,
    height DECIMAL(5,2) NOT NULL,
    weight DECIMAL(5,2) NOT NULL,
    gender VARCHAR(10) NOT NULL,
    activity_level VARCHAR(50) NOT NULL,
    goal VARCHAR(50) NOT NULL,
    tdee DECIMAL(8,2) NOT NULL,
    adjusted_calories DECIMAL(8,2) NOT NULL,
    calculation_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Achievements tablosu
CREATE TABLE IF NOT EXISTS achievements (
    achievement_id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(user_id) ON DELETE CASCADE,
    achievement_name VARCHAR(100) NOT NULL,
    achievement_description TEXT,
    achieved_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- User Settings tablosu
CREATE TABLE IF NOT EXISTS user_settings (
    setting_id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(user_id) ON DELETE CASCADE,
    daily_goal INTEGER DEFAULT 30,
    is_dark_mode BOOLEAN DEFAULT FALSE,
    notifications_enabled BOOLEAN DEFAULT FALSE,
    notification_time TIME DEFAULT '09:00:00',
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- İndeksler
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_exercises_user_id ON exercises(user_id);
CREATE INDEX IF NOT EXISTS idx_exercises_date ON exercises(exercise_date);
CREATE INDEX IF NOT EXISTS idx_calorie_calculations_user_id ON calorie_calculations(user_id);
CREATE INDEX IF NOT EXISTS idx_achievements_user_id ON achievements(user_id);
CREATE INDEX IF NOT EXISTS idx_user_settings_user_id ON user_settings(user_id);

-- Trigger fonksiyonu (updated_at için)
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger'lar
CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_user_settings_updated_at
    BEFORE UPDATE ON user_settings
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column(); 