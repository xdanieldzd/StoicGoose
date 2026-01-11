using System;

namespace StoicGoose.WinForms.XInput
{
    public static class ControllerManager
    {
        public const int MaxControllers = 4;

        readonly static Controller[] controllers;

        static ControllerManager()
        {
            controllers = new Controller[MaxControllers];
            for (int i = 0; i < controllers.Length; i++)
                controllers[i] = new(i);
        }

        public static Controller GetController(int index)
        {
            if (index < 0 || index >= MaxControllers) throw new Exception("Controller index out of range");
            return controllers[index];
        }

        public static void Update()
        {
            for (int i = 0; i < controllers.Length; i++)
                controllers[i].Update();
        }
    }
}
