import { Button } from "@/components/ui/button";

export default function Home() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center p-8 bg-zinc-50 dark:bg-zinc-950 font-sans">
      <main className="max-w-xl w-full bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-xl p-8 shadow-sm flex flex-col items-center text-center space-y-6">
        <div className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300">
          Setup Complete
        </div>

        <h1 className="text-3xl font-bold tracking-tight text-zinc-900 dark:text-zinc-50">
          Project 02 — Team Task Manager
        </h1>

        <p className="text-sm text-zinc-600 dark:text-zinc-400">
          Next.js App Router + Tailwind CSS + shadcn/ui is configured and ready.
        </p>

        <div className="flex items-center gap-3">
          <Button variant="default">shadcn Button</Button>
          <Button variant="outline">Outline</Button>
        </div>
      </main>
    </div>
  );
}
