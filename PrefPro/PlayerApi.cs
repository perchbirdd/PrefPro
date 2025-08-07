using FFXIVClientStructs.FFXIV.Client.Game.UI; 
 
namespace PrefPro; 
 
public static unsafe class PlayerApi 
{ 
    public static string CharacterName => PlayerState.Instance()->CharacterNameString; 
    public static ref ulong ContentId => ref PlayerState.Instance()->ContentId; 
    public static ref byte Sex => ref PlayerState.Instance()->Sex; 
    public static ref byte Race => ref PlayerState.Instance()->Race; 
    public static ref byte Tribe => ref PlayerState.Instance()->Tribe; 
}