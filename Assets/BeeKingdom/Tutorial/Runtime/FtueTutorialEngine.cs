using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Tutorial
{
    public interface ITutorialProgressStore
    {
        FtueProgress LoadLocal();
        void SaveLocal(FtueProgress progress);
        // Server is async via TutorialClient — engine will call it separately
    }

    public sealed class PlayerPrefsTutorialStore : ITutorialProgressStore
    {
        private const string Key = "BeeKingdom_FTUE_Progress_v1";
        public FtueProgress LoadLocal()
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<FtueProgress>(json); } catch { return null; }
        }
        public void SaveLocal(FtueProgress progress)
        {
            if (progress == null) return;
            string json = JsonUtility.ToJson(progress);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
    }

    // Non-Mono engine — testable
    public sealed class FtueTutorialEngine
    {
        private readonly Dictionary<string, FtueChapterDefinition> _chapters;
        private FtueProgress _progress;
        private FtueChapterDefinition _currentChapter;
        private FtueStepDefinition _currentStep;
        private readonly ITutorialProgressStore _store;
        private bool _completedNotified;

        public event Action<FtueStepDefinition> StepEntered;
        public event Action<FtueStepDefinition> StepCompleted;
        public event Action<string> ChapterCompleted;

        public FtueProgress Progress => _progress;
        public FtueStepDefinition CurrentStep => _currentStep;
        public string CurrentChapterId => _currentChapter?.ChapterId;

        public FtueTutorialEngine(Dictionary<string, FtueChapterDefinition> chapters, ITutorialProgressStore store, FtueProgress initialProgress)
        {
            _chapters = chapters ?? FtueChapterDefinitions.All;
            _store = store ?? new PlayerPrefsTutorialStore();
            _progress = initialProgress ?? _store.LoadLocal() ?? new FtueProgress();
        }

        public bool TryStartChapter(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId)) return false;
            if (_progress.IsChapterComplete(chapterId)) return false;
            if (!_chapters.TryGetValue(chapterId, out var ch)) return false;
            _currentChapter = ch;
            _progress.ChapterId = chapterId;
            string resume = !string.IsNullOrEmpty(_progress.CurrentStepId) && ch.FindStep(_progress.CurrentStepId) != null ? _progress.CurrentStepId : ch.EntryStepId;
            return TryEnterStep(resume);
        }

        public bool TryEnterStep(string stepId)
        {
            if (_currentChapter == null) return false;
            var step = _currentChapter.FindStep(stepId);
            if (step == null) return false;
            if (_progress.IsStepCompleted(stepId)) // idempotence — skip already completed, go next
            {
                if (!string.IsNullOrEmpty(step.NextStepId)) return TryEnterStep(step.NextStepId);
                return CompleteChapter();
            }
            _currentStep = step;
            _progress.CurrentStepId = stepId;
            _progress.UpdatedAtUtc = DateTimeOffset.UtcNow;
            StepEntered?.Invoke(step);
            return true;
        }

        public bool NotifyEvent(FtueEventKind kind, string param = null)
        {
            if (_currentStep == null) return false;
            if (_currentStep.CompletionEvent != kind) return false;
            if (!string.IsNullOrEmpty(_currentStep.CompletionEventParam) && !string.Equals(_currentStep.CompletionEventParam, param, StringComparison.Ordinal)) return false;
            if (_progress.IsStepCompleted(_currentStep.StepId)) return false; // idempotence
            return CompleteCurrentStep();
        }

        private bool CompleteCurrentStep()
        {
            if (_currentStep == null) return false;
            string completedId = _currentStep.StepId;
            if (_progress.CompletedSteps.Contains(completedId)) return false; // double-click guard
            _progress.CompletedSteps.Add(completedId);
            _progress.LastCompletedStepId = completedId;
            _progress.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var completed = _currentStep;
            StepCompleted?.Invoke(completed);
            _store.SaveLocal(_progress);

            // persist server async — fire and forget via TutorialClient if available (attached via bootstrap)
            string next = completed.NextStepId;
            if (string.IsNullOrEmpty(next))
            {
                return CompleteChapter();
            }
            _currentStep = null;
            return TryEnterStep(next);
        }

        private bool CompleteChapter()
        {
            if (_currentChapter == null) return false;
            string cid = _currentChapter.ChapterId;
            if (!_progress.CompletedChapters.Contains(cid)) _progress.CompletedChapters.Add(cid);
            _progress.CurrentStepId = string.Empty;
            _progress.UpdatedAtUtc = DateTimeOffset.UtcNow;
            _store.SaveLocal(_progress);
            ChapterCompleted?.Invoke(cid);
            _currentStep = null;
            _currentChapter = null;
            return true;
        }

        // For tests / dev reset
        public void ForceReset()
        {
            _progress = new FtueProgress();
            _currentChapter = null;
            _currentStep = null;
            _store.SaveLocal(_progress);
        }
    }
}
