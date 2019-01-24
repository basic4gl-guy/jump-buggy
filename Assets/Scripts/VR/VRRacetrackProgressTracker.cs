using System.Collections;

public class VRRacetrackProgressTracker : RacetrackProgressTracker
{
    private bool isPuttingCarOnRoad = false;

    public override IEnumerator PutCarOnRoadCoroutine()
    {
        // Abort if already in the process of placing the car on the road
        if (isPuttingCarOnRoad)
            return RacetrackCoroutineUtil.EmptyCoroutine();

        isPuttingCarOnRoad = true;
        return VRCoroutineUtil.FadeOut()
            .Then(() => {
                PutCarOnRoad();
                isPuttingCarOnRoad = false;
            })
            .Then(VRCoroutineUtil.FadeIn());
    }
}
