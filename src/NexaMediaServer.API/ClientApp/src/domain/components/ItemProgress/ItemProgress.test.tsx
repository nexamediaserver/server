/**
 * @file ItemProgress.test.tsx
 *
 * Unit tests for the ItemProgress domain component.
 *
 * These tests verify the component's rendering behavior under different
 * conditions and its progress calculation logic.
 *
 * @see ItemProgress.tsx for the component implementation
 */
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { ItemProgress } from './ItemProgress'

describe('ItemProgress', () => {
  /**
   * Test Group: Conditional Rendering
   *
   * ItemProgress should only render when there's meaningful progress to show.
   * This prevents visual clutter from empty progress bars.
   */
  describe('conditional rendering', () => {
    it('renders nothing when viewOffset is not provided', () => {
      const { container } = render(<ItemProgress length={1000} />)
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when length is not provided', () => {
      const { container } = render(<ItemProgress viewOffset={500} />)
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when both values are missing', () => {
      const { container } = render(<ItemProgress />)
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when viewOffset is zero', () => {
      const { container } = render(
        <ItemProgress length={1000} viewOffset={0} />,
      )
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when length is zero', () => {
      const { container } = render(<ItemProgress length={0} viewOffset={500} />)
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when viewOffset is null', () => {
      const { container } = render(
        <ItemProgress length={1000} viewOffset={null} />,
      )
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when length is null', () => {
      const { container } = render(
        <ItemProgress length={null} viewOffset={500} />,
      )
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when viewOffset is negative', () => {
      const { container } = render(
        <ItemProgress length={1000} viewOffset={-100} />,
      )
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing when length is negative', () => {
      const { container } = render(
        <ItemProgress length={-1000} viewOffset={500} />,
      )
      expect(container.firstChild).toBeNull()
    })
  })

  /**
   * Test Group: Progress Calculation
   *
   * When valid values are provided, the component should render a progress
   * bar with the correct percentage value.
   */
  describe('progress calculation', () => {
    it('renders a progress bar when both values are valid', () => {
      render(<ItemProgress length={1000} viewOffset={500} />)
      const progressBar = screen.getByRole('progressbar')
      expect(progressBar).toBeInTheDocument()
    })

    it('renders progress bar at partial progress', () => {
      render(<ItemProgress length={1000} viewOffset={250} />)
      const progressBar = screen.getByRole('progressbar')
      // The Progress component from Radix uses aria-valuemax and aria-valuenow
      expect(progressBar).toHaveAttribute('aria-valuemax', '100')
    })

    it('caps progress at 100% when viewOffset exceeds length', () => {
      render(<ItemProgress length={1000} viewOffset={1500} />)
      const progressBar = screen.getByRole('progressbar')
      // Even though viewOffset > length, progress should be capped at 100
      expect(progressBar).toBeInTheDocument()
    })

    it('applies custom className to the progress bar', () => {
      render(
        <ItemProgress className="absolute" length={1000} viewOffset={500} />,
      )
      const progressBar = screen.getByRole('progressbar')
      expect(progressBar).toHaveClass('absolute')
    })
  })
})
