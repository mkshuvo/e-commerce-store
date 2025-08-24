-- E-Commerce Store Database Initialization Script
-- This script sets up the initial database structure and configurations

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create schemas
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS products;
CREATE SCHEMA IF NOT EXISTS orders;
CREATE SCHEMA IF NOT EXISTS payments;
CREATE SCHEMA IF NOT EXISTS notifications;
CREATE SCHEMA IF NOT EXISTS audit;

-- Set search path
SET search_path TO public, auth, products, orders, payments, notifications, audit;

-- Create audit log table
CREATE TABLE IF NOT EXISTS audit.audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    table_name VARCHAR(100) NOT NULL,
    operation VARCHAR(10) NOT NULL,
    old_values JSONB,
    new_values JSONB,
    user_id UUID,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ip_address INET,
    user_agent TEXT
);

-- Create indexes for audit logs
CREATE INDEX IF NOT EXISTS idx_audit_logs_table_name ON audit.audit_logs(table_name);
CREATE INDEX IF NOT EXISTS idx_audit_logs_timestamp ON audit.audit_logs(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_logs_user_id ON audit.audit_logs(user_id);

-- Create audit trigger function
CREATE OR REPLACE FUNCTION audit.audit_trigger_function()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit.audit_logs (table_name, operation, old_values)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(OLD));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit.audit_logs (table_name, operation, old_values, new_values)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(OLD), row_to_json(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit.audit_logs (table_name, operation, new_values)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(NEW));
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create function to add audit triggers to tables
CREATE OR REPLACE FUNCTION audit.add_audit_trigger(table_name TEXT)
RETURNS VOID AS $$
BEGIN
    EXECUTE format('CREATE TRIGGER audit_trigger_%s
                    AFTER INSERT OR UPDATE OR DELETE ON %s
                    FOR EACH ROW EXECUTE FUNCTION audit.audit_trigger_function();',
                   replace(table_name, '.', '_'), table_name);
END;
$$ LANGUAGE plpgsql;

-- Create users table in auth schema
CREATE TABLE IF NOT EXISTS auth.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone_number VARCHAR(20),
    date_of_birth DATE,
    is_email_verified BOOLEAN DEFAULT FALSE,
    is_phone_verified BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    role VARCHAR(50) DEFAULT 'Customer',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP WITH TIME ZONE,
    failed_login_attempts INTEGER DEFAULT 0,
    locked_until TIMESTAMP WITH TIME ZONE,
    password_reset_token VARCHAR(255),
    password_reset_expires TIMESTAMP WITH TIME ZONE,
    email_verification_token VARCHAR(255),
    email_verification_expires TIMESTAMP WITH TIME ZONE
);

-- Create indexes for users table
CREATE INDEX IF NOT EXISTS idx_users_email ON auth.users(email);
CREATE INDEX IF NOT EXISTS idx_users_role ON auth.users(role);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON auth.users(is_active);
CREATE INDEX IF NOT EXISTS idx_users_created_at ON auth.users(created_at);

-- Create user addresses table
CREATE TABLE IF NOT EXISTS auth.user_addresses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    address_type VARCHAR(20) DEFAULT 'shipping', -- shipping, billing
    street_address VARCHAR(255) NOT NULL,
    city VARCHAR(100) NOT NULL,
    state_province VARCHAR(100),
    postal_code VARCHAR(20) NOT NULL,
    country VARCHAR(100) NOT NULL,
    is_default BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for user addresses
CREATE INDEX IF NOT EXISTS idx_user_addresses_user_id ON auth.user_addresses(user_id);
CREATE INDEX IF NOT EXISTS idx_user_addresses_type ON auth.user_addresses(address_type);

-- Create categories table
CREATE TABLE IF NOT EXISTS products.categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    parent_id UUID REFERENCES products.categories(id),
    slug VARCHAR(100) UNIQUE NOT NULL,
    image_url VARCHAR(500),
    is_active BOOLEAN DEFAULT TRUE,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for categories
CREATE INDEX IF NOT EXISTS idx_categories_slug ON products.categories(slug);
CREATE INDEX IF NOT EXISTS idx_categories_parent_id ON products.categories(parent_id);
CREATE INDEX IF NOT EXISTS idx_categories_is_active ON products.categories(is_active);

-- Create products table
CREATE TABLE IF NOT EXISTS products.products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    short_description VARCHAR(500),
    sku VARCHAR(100) UNIQUE NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    compare_at_price DECIMAL(10,2),
    cost_price DECIMAL(10,2),
    weight DECIMAL(8,2),
    dimensions JSONB, -- {"length": 10, "width": 5, "height": 3}
    category_id UUID REFERENCES products.categories(id),
    brand VARCHAR(100),
    tags TEXT[],
    images JSONB, -- Array of image URLs
    is_active BOOLEAN DEFAULT TRUE,
    is_featured BOOLEAN DEFAULT FALSE,
    requires_shipping BOOLEAN DEFAULT TRUE,
    is_digital BOOLEAN DEFAULT FALSE,
    stock_quantity INTEGER DEFAULT 0,
    low_stock_threshold INTEGER DEFAULT 10,
    manage_stock BOOLEAN DEFAULT TRUE,
    allow_backorders BOOLEAN DEFAULT FALSE,
    seo_title VARCHAR(255),
    seo_description VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for products
CREATE INDEX IF NOT EXISTS idx_products_sku ON products.products(sku);
CREATE INDEX IF NOT EXISTS idx_products_category_id ON products.products(category_id);
CREATE INDEX IF NOT EXISTS idx_products_is_active ON products.products(is_active);
CREATE INDEX IF NOT EXISTS idx_products_is_featured ON products.products(is_featured);
CREATE INDEX IF NOT EXISTS idx_products_price ON products.products(price);
CREATE INDEX IF NOT EXISTS idx_products_name_trgm ON products.products USING gin(name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_products_tags ON products.products USING gin(tags);

-- Create product variants table
CREATE TABLE IF NOT EXISTS products.product_variants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products.products(id) ON DELETE CASCADE,
    sku VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    price DECIMAL(10,2),
    compare_at_price DECIMAL(10,2),
    cost_price DECIMAL(10,2),
    weight DECIMAL(8,2),
    stock_quantity INTEGER DEFAULT 0,
    attributes JSONB, -- {"color": "red", "size": "large"}
    image_url VARCHAR(500),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for product variants
CREATE INDEX IF NOT EXISTS idx_product_variants_product_id ON products.product_variants(product_id);
CREATE INDEX IF NOT EXISTS idx_product_variants_sku ON products.product_variants(sku);
CREATE INDEX IF NOT EXISTS idx_product_variants_is_active ON products.product_variants(is_active);

-- Create orders table
CREATE TABLE IF NOT EXISTS orders.orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_number VARCHAR(50) UNIQUE NOT NULL,
    user_id UUID REFERENCES auth.users(id),
    status VARCHAR(50) DEFAULT 'pending', -- pending, confirmed, processing, shipped, delivered, cancelled, refunded
    payment_status VARCHAR(50) DEFAULT 'pending', -- pending, paid, failed, refunded, partially_refunded
    fulfillment_status VARCHAR(50) DEFAULT 'unfulfilled', -- unfulfilled, partial, fulfilled
    subtotal DECIMAL(10,2) NOT NULL,
    tax_amount DECIMAL(10,2) DEFAULT 0,
    shipping_amount DECIMAL(10,2) DEFAULT 0,
    discount_amount DECIMAL(10,2) DEFAULT 0,
    total_amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    shipping_address JSONB,
    billing_address JSONB,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    shipped_at TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE
);

-- Create indexes for orders
CREATE INDEX IF NOT EXISTS idx_orders_order_number ON orders.orders(order_number);
CREATE INDEX IF NOT EXISTS idx_orders_user_id ON orders.orders(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders.orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_payment_status ON orders.orders(payment_status);
CREATE INDEX IF NOT EXISTS idx_orders_created_at ON orders.orders(created_at);

-- Create order items table
CREATE TABLE IF NOT EXISTS orders.order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders.orders(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products.products(id),
    product_variant_id UUID REFERENCES products.product_variants(id),
    product_name VARCHAR(255) NOT NULL,
    product_sku VARCHAR(100) NOT NULL,
    quantity INTEGER NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL,
    total_price DECIMAL(10,2) NOT NULL,
    product_snapshot JSONB, -- Store product details at time of order
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for order items
CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON orders.order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_order_items_product_id ON orders.order_items(product_id);

-- Create payments table
CREATE TABLE IF NOT EXISTS payments.payments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID REFERENCES orders.orders(id),
    payment_method VARCHAR(50) NOT NULL, -- stripe, paypal, bank_transfer
    payment_provider VARCHAR(50) NOT NULL,
    provider_payment_id VARCHAR(255),
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    status VARCHAR(50) DEFAULT 'pending', -- pending, processing, succeeded, failed, cancelled, refunded
    failure_reason TEXT,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP WITH TIME ZONE
);

-- Create indexes for payments
CREATE INDEX IF NOT EXISTS idx_payments_order_id ON payments.payments(order_id);
CREATE INDEX IF NOT EXISTS idx_payments_status ON payments.payments(status);
CREATE INDEX IF NOT EXISTS idx_payments_provider_payment_id ON payments.payments(provider_payment_id);

-- Create notifications table
CREATE TABLE IF NOT EXISTS notifications.notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES auth.users(id),
    type VARCHAR(50) NOT NULL, -- email, sms, push
    channel VARCHAR(50) NOT NULL, -- order_confirmation, shipping_update, etc.
    recipient VARCHAR(255) NOT NULL,
    subject VARCHAR(255),
    content TEXT NOT NULL,
    status VARCHAR(50) DEFAULT 'pending', -- pending, sent, failed, delivered
    provider VARCHAR(50), -- sendgrid, twilio, etc.
    provider_message_id VARCHAR(255),
    error_message TEXT,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    sent_at TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE
);

-- Create indexes for notifications
CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON notifications.notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_status ON notifications.notifications(status);
CREATE INDEX IF NOT EXISTS idx_notifications_type ON notifications.notifications(type);
CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON notifications.notifications(created_at);

-- Add audit triggers to all tables
SELECT audit.add_audit_trigger('auth.users');
SELECT audit.add_audit_trigger('auth.user_addresses');
SELECT audit.add_audit_trigger('products.categories');
SELECT audit.add_audit_trigger('products.products');
SELECT audit.add_audit_trigger('products.product_variants');
SELECT audit.add_audit_trigger('orders.orders');
SELECT audit.add_audit_trigger('orders.order_items');
SELECT audit.add_audit_trigger('payments.payments');
SELECT audit.add_audit_trigger('notifications.notifications');

-- Create updated_at trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Add updated_at triggers to tables that have updated_at column
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON auth.users FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_user_addresses_updated_at BEFORE UPDATE ON auth.user_addresses FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_categories_updated_at BEFORE UPDATE ON products.categories FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_products_updated_at BEFORE UPDATE ON products.products FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_product_variants_updated_at BEFORE UPDATE ON products.product_variants FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_orders_updated_at BEFORE UPDATE ON orders.orders FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_payments_updated_at BEFORE UPDATE ON payments.payments FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Grant permissions
GRANT USAGE ON SCHEMA auth TO ecommerce_user;
GRANT USAGE ON SCHEMA products TO ecommerce_user;
GRANT USAGE ON SCHEMA orders TO ecommerce_user;
GRANT USAGE ON SCHEMA payments TO ecommerce_user;
GRANT USAGE ON SCHEMA notifications TO ecommerce_user;
GRANT USAGE ON SCHEMA audit TO ecommerce_user;

GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA auth TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA products TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA orders TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA payments TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA notifications TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA audit TO ecommerce_user;

GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA auth TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA products TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA orders TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA payments TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA notifications TO ecommerce_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA audit TO ecommerce_user;

-- Create function to generate order numbers
CREATE OR REPLACE FUNCTION generate_order_number()
RETURNS TEXT AS $$
DECLARE
    order_num TEXT;
    counter INTEGER;
BEGIN
    -- Generate order number with format: ORD-YYYYMMDD-NNNN
    SELECT COALESCE(MAX(CAST(SUBSTRING(order_number FROM 13) AS INTEGER)), 0) + 1
    INTO counter
    FROM orders.orders
    WHERE order_number LIKE 'ORD-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-%';
    
    order_num := 'ORD-' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') || '-' || LPAD(counter::TEXT, 4, '0');
    
    RETURN order_num;
END;
$$ LANGUAGE plpgsql;

-- Create trigger to auto-generate order numbers
CREATE OR REPLACE FUNCTION set_order_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.order_number IS NULL OR NEW.order_number = '' THEN
        NEW.order_number := generate_order_number();
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER set_order_number_trigger
    BEFORE INSERT ON orders.orders
    FOR EACH ROW
    EXECUTE FUNCTION set_order_number();

-- Insert initial data
INSERT INTO auth.users (email, password_hash, first_name, last_name, role, is_email_verified, is_active)
VALUES 
    ('admin@ecommerce.local', '$2a$11$dummy.hash.for.admin.user', 'Admin', 'User', 'Admin', true, true),
    ('manager@ecommerce.local', '$2a$11$dummy.hash.for.manager.user', 'Manager', 'User', 'Manager', true, true)
ON CONFLICT (email) DO NOTHING;

-- Insert sample categories
INSERT INTO products.categories (name, description, slug, is_active, sort_order)
VALUES 
    ('Electronics', 'Electronic devices and accessories', 'electronics', true, 1),
    ('Clothing', 'Fashion and apparel', 'clothing', true, 2),
    ('Home & Garden', 'Home improvement and garden supplies', 'home-garden', true, 3),
    ('Books', 'Books and educational materials', 'books', true, 4),
    ('Sports & Outdoors', 'Sports equipment and outdoor gear', 'sports-outdoors', true, 5)
ON CONFLICT (slug) DO NOTHING;

COMMIT;