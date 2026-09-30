import CompraiWidget from '@/components/CompraiWidget'

export default function WidgetPage() {
  return (
    <div style={{ minHeight: '100vh', background: '#f3f4f6', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <div style={{ textAlign: 'center', color: '#6b7280' }}>
        <p style={{ fontSize: 14 }}>Demo do widget embeddável</p>
        <p style={{ fontSize: 12, marginTop: 4 }}>Clique no botão 🛍️ no canto inferior direito</p>
        <code style={{ fontSize: 11, background: '#e5e7eb', padding: '4px 8px', borderRadius: 4, marginTop: 8, display: 'inline-block' }}>
          {'<script src="https://comprai.seudominio.com/widget.js"></script>'}
        </code>
      </div>
      <CompraiWidget />
    </div>
  )
}
