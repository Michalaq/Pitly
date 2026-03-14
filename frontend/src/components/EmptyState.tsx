import { useNavigate } from 'react-router-dom';

export default function EmptyState() {
  const navigate = useNavigate();
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] text-slate-400">
      <p className="mb-4">No data imported yet.</p>
      <button onClick={() => navigate('/')} className="bg-blue-600 hover:bg-blue-500 text-white font-medium px-4 py-2 rounded-lg transition-colors">
        Import Statement
      </button>
    </div>
  );
}
