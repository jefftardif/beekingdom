namespace BeeKingdom.Playground
{
    // Pure state machine: shared pointer ownership survives a modal disappearing.
    // Callers pass aggregate held state (mouse buttons and every active touch).
    public sealed class ModalGestureBarrier
    {
        private bool wasOpen;
        private bool captured;
        private int throughFrame = -1;
        private int lastFrame = -1;

        public void Reset()
        {
            wasOpen = captured = false;
            throughFrame = lastFrame = -1;
        }

        private void Advance(int frame)
        {
            if (frame < lastFrame) Reset();
            lastFrame = frame;
        }

        public void Capture(int frame)
        {
            Advance(frame);
            captured = true;
            if (throughFrame < frame + 1) throughFrame = frame + 1;
        }

        public bool Observe(bool modalOpen, bool anyPointerHeld, int frame)
        {
            Advance(frame);
            if ((modalOpen && anyPointerHeld) || (wasOpen && !modalOpen)) captured = true;
            wasOpen = modalOpen;
            if (captured && !anyPointerHeld)
            {
                captured = false;
                if (throughFrame < frame + 1) throughFrame = frame + 1;
            }
            return modalOpen || captured || frame <= throughFrame;
        }
    }
}
