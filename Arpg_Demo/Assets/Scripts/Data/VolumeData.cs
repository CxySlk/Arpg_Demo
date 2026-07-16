using MessagePack;

[MessagePackObject]
public class VolumeData
{
    [Key(0)]
    public float bgVolume = .5f;
    [Key(1)]
    public float effVolume = .5f;
    [Key(2)]
    public float globalVolume = .5f;
}
