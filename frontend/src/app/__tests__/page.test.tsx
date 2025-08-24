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

  it('has proper document structure', () => {
    render(<Home />);
    
    // Check for main content area
    const main = screen.getByRole('main');
    expect(main).toBeInTheDocument();
    expect(main).toHaveClass('flex', 'flex-col', 'gap-[32px]', 'row-start-2', 'items-center', 'sm:items-start');
  });

  it('contains Next.js logo', () => {
    render(<Home />);
    
    // Check for Next.js logo
    const logo = screen.getByAltText('Next.js logo');
    expect(logo).toBeInTheDocument();
  });

  it('has proper styling classes', () => {
    render(<Home />);

    const main = screen.getByRole('main');
    expect(main).toHaveClass('flex', 'flex-col', 'gap-[32px]', 'row-start-2', 'items-center', 'sm:items-start');
  });

  it('renders all main sections', () => {
    render(<Home />);
    
    // Check for main sections based on the default Next.js structure
    const main = screen.getByRole('main');
    expect(main).toBeInTheDocument();
    
    // The component should have the basic structure
    expect(main.children.length).toBeGreaterThan(0);
  });
});