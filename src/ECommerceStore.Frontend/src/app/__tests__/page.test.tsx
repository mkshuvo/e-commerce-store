import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import Home from '../page';

// Mock Next.js Image component
jest.mock('next/image', () => {
  return function MockImage({ src, alt, ...props }: React.ImgHTMLAttributes<HTMLImageElement>) {
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={src} alt={alt} {...props} />;
  };
});

describe('Home Page', () => {
  it('renders without crashing', () => {
    render(<Home />);
    
    // Check if the main element is present
    const main = screen.getByRole('main');
    expect(main).toBeInTheDocument();
  });

  it('displays the store title', () => {
    render(<Home />);
    
    // Check for the store title
    const title = screen.getByText('E-Commerce Store');
    expect(title).toBeInTheDocument();
  });

  it('displays the welcome message', () => {
    render(<Home />);
    
    // Check for welcome heading
    const welcomeHeading = screen.getByText('Welcome to Our Store');
    expect(welcomeHeading).toBeInTheDocument();
  });

  it('has proper main styling classes', () => {
    render(<Home />);

    const main = screen.getByRole('main');
    expect(main).toHaveClass('max-w-7xl', 'mx-auto', 'px-4', 'sm:px-6', 'lg:px-8', 'py-12');
  });

  it('renders navigation links', () => {
    render(<Home />);
    
    // Check for navigation links
    const productsLink = screen.getByText('Products');
    const categoriesLink = screen.getByText('Categories');
    const aboutLink = screen.getByText('About');
    const contactLink = screen.getByText('Contact');
    
    expect(productsLink).toBeInTheDocument();
    expect(categoriesLink).toBeInTheDocument();
    expect(aboutLink).toBeInTheDocument();
    expect(contactLink).toBeInTheDocument();
  });

  it('renders feature cards', () => {
    render(<Home />);
    
    // Check for feature cards
    const fastShipping = screen.getByText('Fast Shipping');
    const qualityProducts = screen.getByText('Quality Products');
    const support = screen.getByText('24/7 Support');
    
    expect(fastShipping).toBeInTheDocument();
    expect(qualityProducts).toBeInTheDocument();
    expect(support).toBeInTheDocument();
  });

  it('renders call-to-action buttons', () => {
    render(<Home />);
    
    // Check for CTA buttons
    const shopNowButton = screen.getByText('Shop Now');
    const learnMoreButton = screen.getByText('Learn More');
    
    expect(shopNowButton).toBeInTheDocument();
    expect(learnMoreButton).toBeInTheDocument();
  });

  it('renders footer with copyright', () => {
    render(<Home />);
    
    // Check for footer content
    const copyright = screen.getByText('© 2024 E-Commerce Store. All rights reserved.');
    expect(copyright).toBeInTheDocument();
  });
});