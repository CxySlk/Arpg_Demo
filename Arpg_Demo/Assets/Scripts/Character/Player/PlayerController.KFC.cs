    using Character;

    public partial class PlayerController
    {
        private KungFuCircle _kungFuCircle;
        public KungFuCircle KFC => _kungFuCircle;

        private StageController _stageController;
        public StageController StageController => _stageController;
    }
