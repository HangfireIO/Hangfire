namespace HangFire.Common.States
{
    public class StateHandler
    {
        public virtual void OnApply(StateApplyingContext context, string stateName)
        {
        }

        public virtual void OnUnapply(StateApplyingContext context, string stateName)
        {
        }
    }
}
