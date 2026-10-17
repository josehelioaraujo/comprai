import type { Config } from 'tailwindcss'

const config: Config = {
  content: ['./src/**/*.{ts,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: '#7C3AED',
          light:   '#EDE9FE',
          dark:    '#5B21B6',
        },
        surface: '#F9FAFB',
        ink:     '#111111',
        muted:   '#6B7280',
        border:  '#E5E7EB',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        '2xl': '1rem',
        '3xl': '1.5rem',
      },
      boxShadow: {
        card: '0 1px 3px 0 rgb(0 0 0 / .08), 0 1px 2px -1px rgb(0 0 0 / .06)',
        'card-hover': '0 4px 12px 0 rgb(0 0 0 / .10)',
      },
      height: {
        nav: '64px',
      },
    },
  },
  plugins: [],
}

export default config
