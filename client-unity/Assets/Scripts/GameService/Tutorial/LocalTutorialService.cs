using System;
using System.Collections.Generic;
using System.Linq;
using FishingIdle.GameService.Core;
using FishingIdle.GameService.Persistence;

namespace FishingIdle.GameService.Tutorial
{
    /// <summary>The tutorial steps in order (GDD section 40). Technical keys; the texts live in Texts.</summary>
    public static class TutorialSteps
    {
        public const string Welcome = "welcome";
        public const string ClaimRod = "claim_rod";
        public const string StartFishing = "start_fishing";
        public const string FirstCatch = "first_catch";
        public const string OpenBox = "open_box";
        public const string SellFish = "sell_fish";
        public const string KeepFish = "keep_fish";
        public const string Cardume = "cardume";
        public const string Expedition = "expedition";
        public const string Done = "done";

        public static readonly IReadOnlyList<string> Order = new[]
        {
            Welcome, ClaimRod, StartFishing, FirstCatch, OpenBox, SellFish, KeepFish, Cardume, Expedition,
        };

        /// <summary>Steps the player finishes by reading or looking, not by doing something in the rules.</summary>
        public static bool IsAcknowledged(string step) => step == Welcome || step == OpenBox || step == Expedition;
    }

    public sealed class TutorialView
    {
        /// <summary>False once completed or skipped.</summary>
        public bool Active { get; internal set; }

        public string Step { get; internal set; }

        /// <summary>1-based position of the step, and how many there are.</summary>
        public int StepNumber { get; internal set; }
        public int StepCount { get; internal set; }

        /// <summary>The step ends when the client reports it was seen (Acknowledge).</summary>
        public bool NeedsAcknowledge { get; internal set; }
    }

    /// <summary>
    /// A short teach-by-doing tutorial. Most steps finish on their own when the save shows the player
    /// did the thing (claimed the rod, caught, sold, kept, used the Cardume); a few are just read.
    /// </summary>
    public interface ITutorialService
    {
        /// <summary>Advances past any step already done, then describes the current one.</summary>
        TutorialView Get();

        /// <summary>Ends a read-only step (welcome, Fishing Box, Expedition intro). Must be the current one.</summary>
        ServiceResult<TutorialView> Acknowledge(string step);

        /// <summary>Ends the tutorial now. A player without a rod gets the Starter Rod, so nobody is left stuck.</summary>
        TutorialView Skip();
    }

    public sealed class LocalTutorialService : ITutorialService
    {
        private readonly GameSession _session;

        public LocalTutorialService(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        private PlayerSave Save => _session.Save;
        private TutorialState State => Save.Tutorial;

        public TutorialView Get()
        {
            if (Advance())
            {
                _session.Persist();
            }

            return View();
        }

        public ServiceResult<TutorialView> Acknowledge(string step)
        {
            Advance();
            if (State.Completed || State.Step != step || !TutorialSteps.IsAcknowledged(step))
            {
                return ServiceResult<TutorialView>.Fail(ServiceError.TutorialStepMismatch);
            }

            MoveNext();
            Advance();
            _session.Persist();
            return ServiceResult<TutorialView>.Ok(View());
        }

        public TutorialView Skip()
        {
            if (!State.Completed)
            {
                State.Completed = true;
                State.Skipped = true;
                State.Step = TutorialSteps.Done;
                if (Save.EquippedRodItem() == null)
                {
                    _session.EquipStarterRod();
                }

                _session.Persist();
                _session.Log("Tutorial skipped.");
            }

            return View();
        }

        /// <summary>Moves past every step whose goal the save already shows. Returns whether anything moved.</summary>
        private bool Advance()
        {
            var moved = false;
            while (!State.Completed && IsDone(State.Step))
            {
                MoveNext();
                moved = true;
            }

            return moved;
        }

        private bool IsDone(string step)
        {
            switch (step)
            {
                case TutorialSteps.ClaimRod: return Save.EquippedRodItem() != null;
                case TutorialSteps.StartFishing: return Save.Fishing.Active || Save.Stats.TotalCatches > 0;
                case TutorialSteps.FirstCatch: return Save.Stats.TotalCatches > 0;
                case TutorialSteps.SellFish: return Save.Stats.FishSold > 0;
                case TutorialSteps.KeepFish: return Save.Aquarium.Count > 0 || Save.CardumeSlots.Any(id => id != 0);
                case TutorialSteps.Cardume: return Save.CardumeSlots.Any(id => id != 0);
                case TutorialSteps.Welcome:
                case TutorialSteps.OpenBox:
                case TutorialSteps.Expedition:
                    return false;
                default:
                    // Unknown key (e.g. a step removed in a later version): finish rather than get stuck.
                    return true;
            }
        }

        private void MoveNext()
        {
            var index = IndexOf(State.Step);
            if (index < 0 || index + 1 >= TutorialSteps.Order.Count)
            {
                State.Step = TutorialSteps.Done;
                State.Completed = true;
                _session.Log("Tutorial completed.");
                return;
            }

            State.Step = TutorialSteps.Order[index + 1];
        }

        private static int IndexOf(string step)
        {
            for (var i = 0; i < TutorialSteps.Order.Count; i++)
            {
                if (TutorialSteps.Order[i] == step)
                {
                    return i;
                }
            }

            return -1;
        }

        private TutorialView View()
        {
            return new TutorialView
            {
                Active = !State.Completed,
                Step = State.Completed ? TutorialSteps.Done : State.Step,
                StepNumber = State.Completed ? TutorialSteps.Order.Count : IndexOf(State.Step) + 1,
                StepCount = TutorialSteps.Order.Count,
                NeedsAcknowledge = !State.Completed && TutorialSteps.IsAcknowledged(State.Step),
            };
        }
    }
}
