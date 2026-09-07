import "./App.css";

function App() {
  return (
    <main className="app-shell">
      <section className="panel">
        <div className="eyebrow">Nexa</div>
        <h1>Server-first media platform</h1>
        <p className="subtitle">
          A single-binary, setup-gated media server with an embedded web client
          and extensible core services.
        </p>

        <div className="status-grid">
          <div className="status-item">
            <span className="label">Mode</span>
            <strong>Bootstrap</strong>
          </div>
          <div className="status-item">
            <span className="label">Storage</span>
            <strong>SQLite first</strong>
          </div>
          <div className="status-item">
            <span className="label">Runtime</span>
            <strong>Go + React</strong>
          </div>
        </div>

        <ul className="feature-list">
          <li>Mandatory first-run setup flow</li>
          <li>Extension-aware capability model</li>
          <li>Server-authoritative playback</li>
          <li>Embedded UI with contract-first API</li>
        </ul>
      </section>
    </main>
  );
}

export default App;
