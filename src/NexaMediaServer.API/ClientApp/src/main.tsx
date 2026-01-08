import { createRoot } from 'react-dom/client'
import '@fontsource-variable/atkinson-hyperlegible-next'

import { App } from '@/app'
import reportWebVitals from '@/app/observability/reportWebVitals'

const rootElement = document.getElementById('root')
if (rootElement && !rootElement.innerHTML) {
  const root = createRoot(rootElement)
  root.render(<App />)
}

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals

// eslint-disable-next-line @typescript-eslint/no-floating-promises -- Expected
reportWebVitals()
