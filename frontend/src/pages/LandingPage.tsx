import { Link } from 'react-router-dom';
import { Upload, ArrowRightLeft, Layers, FileCheck2, Github, Coffee, ArrowRight } from 'lucide-react';

const features = [
  {
    icon: Upload,
    title: 'CSV Import',
    description: 'Drag and drop your broker statement — Interactive Brokers or Trading 212. Pitly handles the rest.',
  },
  {
    icon: ArrowRightLeft,
    title: 'NBP Exchange Rates',
    description: 'Automatic PLN conversion using official NBP mid rates per Polish tax law.',
  },
  {
    icon: Layers,
    title: 'FIFO Capital Gains',
    description: 'Precise lot matching per symbol with first-in-first-out cost basis in PLN.',
  },
  {
    icon: FileCheck2,
    title: 'PIT-38 Ready',
    description: 'Get exact values for every PIT-38 field, ready to enter directly in your tax declaration.',
  },
];

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-slate-900 text-slate-100 flex flex-col">
      <div className="flex-1 flex flex-col items-center justify-center px-6 pt-24 pb-16 relative">
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,_rgba(134,59,255,0.07)_0%,_transparent_70%)]" />
        <div className="relative flex flex-col items-center">
          <img src="/favicon.svg" alt="" className="w-14 h-14 mb-6" />
          <h1 className="text-5xl font-bold tracking-tight mb-4">Pitly</h1>
          <p className="text-xl text-slate-300 text-center max-w-md mb-2">
            PIT-38 tax calculator for
            <br />
            foreign stock investors in Poland
          </p>
          <p className="text-slate-500 text-center max-w-sm mb-6">
            Upload your broker statement CSV and get exact PIT-38 field values in seconds.
          </p>
          <div className="flex items-center gap-2 mb-10">
            <span className="text-xs text-slate-500">Supports:</span>
            <span className="text-xs text-slate-400 bg-slate-800 border border-slate-700 rounded-full px-3 py-1">Interactive Brokers</span>
            <span className="text-xs text-slate-400 bg-slate-800 border border-slate-700 rounded-full px-3 py-1">Trading 212</span>
          </div>
          <Link
            to="/app"
            className="inline-flex items-center gap-2 bg-[#863bff] hover:bg-[#7429ff] text-white font-medium px-7 py-3 rounded-lg transition-colors mb-6"
          >
            Get Started
            <ArrowRight className="w-4 h-4" />
          </Link>
          <a
            href="https://github.com/volodymyr-kovtun/Pitly"
            target="_blank"
            rel="noopener noreferrer"
            className="text-sm text-slate-500 hover:text-slate-300 transition-colors"
          >
            Free & open source — View on GitHub →
          </a>
        </div>
      </div>

      <div className="max-w-3xl mx-auto w-full px-6 pb-24">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {features.map(({ icon: Icon, title, description }) => (
            <div key={title} className="border border-slate-800 rounded-lg p-5">
              <Icon className="w-5 h-5 text-[#863bff] mb-3" />
              <h3 className="font-medium text-sm mb-1">{title}</h3>
              <p className="text-sm text-slate-400 leading-relaxed">{description}</p>
            </div>
          ))}
        </div>
      </div>

      <footer className="border-t border-slate-800 px-6 py-6">
        <div className="max-w-3xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <span className="text-xs text-slate-500">
            MIT License © 2026 Volodymyr Kovtun
          </span>
          <div className="flex items-center gap-5">
            <a
              href="https://github.com/volodymyr-kovtun/Pitly"
              target="_blank"
              rel="noopener noreferrer"
              className="text-slate-500 hover:text-slate-300 transition-colors"
            >
              <Github className="w-4 h-4" />
            </a>
            <a
              href="https://buymeacoffee.com/volodymyr_kovtun"
              target="_blank"
              rel="noopener noreferrer"
              className="text-slate-500 hover:text-slate-300 transition-colors inline-flex items-center gap-1.5 text-xs"
            >
              <Coffee className="w-4 h-4" />
              Buy me a coffee
            </a>
          </div>
        </div>
      </footer>
    </div>
  );
}
