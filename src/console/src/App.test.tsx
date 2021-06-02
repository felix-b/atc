import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';
import { AppServices } from './appServices';

const mockServices: AppServices = {
    trafficService: {} as any,
    worldService: {} as any
};

test('renders learn react link', () => {
    render(<App {...mockServices} />);
    const linkElement = screen.getByText(/learn react/i);
    expect(linkElement).toBeInTheDocument();
});
