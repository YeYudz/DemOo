using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Shared.dll",
		"System.Core.dll",
		"System.dll",
		"UnityEngine.CoreModule.dll",
		"UnityEngine.JSONSerializeModule.dll",
		"YokiFrame.UIKit.dll",
		"YokiFrame.Unity.Runtime.dll",
		"YokiFrame.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// System.Action<DecomposeSystem.MaterialReward>
	// System.Action<GameUI.SettingPanel.ResolutionOption>
	// System.Action<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Action<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Action<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Action<System.ValueTuple<object,float,byte>>
	// System.Action<System.ValueTuple<object,float,int,int>>
	// System.Action<UnityEngine.Vector3>
	// System.Action<int>
	// System.Action<object>
	// System.Collections.Generic.ArraySortHelper<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.ArraySortHelper<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.ArraySortHelper<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ArraySortHelper<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.ArraySortHelper<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.ArraySortHelper<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.ArraySortHelper<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.ArraySortHelper<UnityEngine.Vector3>
	// System.Collections.Generic.ArraySortHelper<int>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.Comparer<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.Comparer<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.Comparer<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.Comparer<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.Comparer<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.Comparer<UnityEngine.Vector3>
	// System.Collections.Generic.Comparer<byte>
	// System.Collections.Generic.Comparer<float>
	// System.Collections.Generic.Comparer<int>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<YokiFrame.ResCacheKey,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,float>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<YokiFrame.ResCacheKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,float>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<YokiFrame.ResCacheKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,float>
	// System.Collections.Generic.Dictionary.KeyCollection<int,int>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,float>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<YokiFrame.ResCacheKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,float>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,float>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<YokiFrame.ResCacheKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,float>
	// System.Collections.Generic.Dictionary.ValueCollection<int,int>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,float>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<YokiFrame.ResCacheKey,object>
	// System.Collections.Generic.Dictionary<int,float>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,float>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<UnityEngine.Vector3>
	// System.Collections.Generic.EqualityComparer<YokiFrame.ResCacheKey>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<float>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.HashSet.Enumerator<int>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<int>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<int>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.ICollection<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<YokiFrame.ResCacheKey,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.ICollection<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.ICollection<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.ICollection<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.ICollection<UnityEngine.Vector3>
	// System.Collections.Generic.ICollection<int>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.IComparer<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.IComparer<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IComparer<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.IComparer<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.IComparer<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.IComparer<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.IComparer<UnityEngine.Vector3>
	// System.Collections.Generic.IComparer<byte>
	// System.Collections.Generic.IComparer<float>
	// System.Collections.Generic.IComparer<int>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IDictionary<object,LitJson.ArrayMetadata>
	// System.Collections.Generic.IDictionary<object,LitJson.ObjectMetadata>
	// System.Collections.Generic.IDictionary<object,LitJson.PropertyMetadata>
	// System.Collections.Generic.IDictionary<object,object>
	// System.Collections.Generic.IEnumerable<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.IEnumerable<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<YokiFrame.ResCacheKey,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.IEnumerable<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.IEnumerable<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.IEnumerable<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.IEnumerable<UnityEngine.Vector3>
	// System.Collections.Generic.IEnumerable<int>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.IEnumerator<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<YokiFrame.ResCacheKey,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,float>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.IEnumerator<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.IEnumerator<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.IEnumerator<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.IEnumerator<UnityEngine.Vector3>
	// System.Collections.Generic.IEnumerator<int>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<YokiFrame.ResCacheKey>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.IList<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IList<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.IList<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.IList<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.IList<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.IList<UnityEngine.Vector3>
	// System.Collections.Generic.IList<int>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<YokiFrame.ResCacheKey,object>
	// System.Collections.Generic.KeyValuePair<int,float>
	// System.Collections.Generic.KeyValuePair<int,int>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,float>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.LinkedList.Enumerator<object>
	// System.Collections.Generic.LinkedList<object>
	// System.Collections.Generic.LinkedListNode<object>
	// System.Collections.Generic.List.Enumerator<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.List.Enumerator<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.List.Enumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.List.Enumerator<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.List.Enumerator<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.List.Enumerator<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.List.Enumerator<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.List.Enumerator<UnityEngine.Vector3>
	// System.Collections.Generic.List.Enumerator<int>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.List<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.List<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.List<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.List<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.List<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.List<UnityEngine.Vector3>
	// System.Collections.Generic.List<int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<DecomposeSystem.MaterialReward>
	// System.Collections.Generic.ObjectComparer<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.Generic.ObjectComparer<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<object,float,byte>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<object,float,int,int>>
	// System.Collections.Generic.ObjectComparer<UnityEngine.Vector3>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<float>
	// System.Collections.Generic.ObjectComparer<int>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<UnityEngine.Vector3>
	// System.Collections.Generic.ObjectEqualityComparer<YokiFrame.ResCacheKey>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<float>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<DecomposeSystem.MaterialReward>
	// System.Collections.ObjectModel.ReadOnlyCollection<GameUI.SettingPanel.ResolutionOption>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.ValueTuple<object,float,byte>>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.ValueTuple<object,float,int,int>>
	// System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Vector3>
	// System.Collections.ObjectModel.ReadOnlyCollection<int>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<DecomposeSystem.MaterialReward>
	// System.Comparison<GameUI.SettingPanel.ResolutionOption>
	// System.Comparison<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Comparison<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Comparison<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Comparison<System.ValueTuple<object,float,byte>>
	// System.Comparison<System.ValueTuple<object,float,int,int>>
	// System.Comparison<UnityEngine.Vector3>
	// System.Comparison<int>
	// System.Comparison<object>
	// System.Converter<System.ValueTuple<object,UnityEngine.Vector3,int>,object>
	// System.Converter<object,object>
	// System.Func<System.Collections.Generic.KeyValuePair<object,float>,float>
	// System.Func<System.Collections.Generic.KeyValuePair<object,float>,int>
	// System.Func<System.Collections.Generic.KeyValuePair<object,int>,byte>
	// System.Func<System.Collections.Generic.KeyValuePair<object,int>,object>
	// System.Func<int,byte>
	// System.Func<int,int>
	// System.Func<object,byte>
	// System.Func<object,float>
	// System.Func<object,int>
	// System.Func<object,object>
	// System.Func<object>
	// System.Linq.Buffer<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Linq.Buffer<int>
	// System.Linq.Buffer<object>
	// System.Linq.Enumerable.<ReverseIterator>d__79<object>
	// System.Linq.Enumerable.<SkipIterator>d__31<object>
	// System.Linq.Enumerable.<TakeIterator>d__25<System.ValueTuple<object,float,int,int>>
	// System.Linq.Enumerable.Iterator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Linq.Enumerable.Iterator<int>
	// System.Linq.Enumerable.Iterator<object>
	// System.Linq.Enumerable.WhereArrayIterator<object>
	// System.Linq.Enumerable.WhereEnumerableIterator<int>
	// System.Linq.Enumerable.WhereEnumerableIterator<object>
	// System.Linq.Enumerable.WhereListIterator<object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<System.Collections.Generic.KeyValuePair<object,int>,object>
	// System.Linq.Enumerable.WhereSelectArrayIterator<object,int>
	// System.Linq.Enumerable.WhereSelectArrayIterator<object,object>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<System.Collections.Generic.KeyValuePair<object,int>,object>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<object,int>
	// System.Linq.Enumerable.WhereSelectEnumerableIterator<object,object>
	// System.Linq.Enumerable.WhereSelectListIterator<System.Collections.Generic.KeyValuePair<object,int>,object>
	// System.Linq.Enumerable.WhereSelectListIterator<object,int>
	// System.Linq.Enumerable.WhereSelectListIterator<object,object>
	// System.Linq.EnumerableSorter<System.Collections.Generic.KeyValuePair<object,float>,float>
	// System.Linq.EnumerableSorter<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Linq.EnumerableSorter<int,int>
	// System.Linq.EnumerableSorter<int>
	// System.Linq.EnumerableSorter<object,byte>
	// System.Linq.EnumerableSorter<object,float>
	// System.Linq.EnumerableSorter<object>
	// System.Linq.IOrderedEnumerable<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Linq.IOrderedEnumerable<object>
	// System.Linq.OrderedEnumerable.<GetEnumerator>d__1<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Linq.OrderedEnumerable.<GetEnumerator>d__1<int>
	// System.Linq.OrderedEnumerable.<GetEnumerator>d__1<object>
	// System.Linq.OrderedEnumerable<System.Collections.Generic.KeyValuePair<object,float>,float>
	// System.Linq.OrderedEnumerable<System.Collections.Generic.KeyValuePair<object,float>>
	// System.Linq.OrderedEnumerable<int,int>
	// System.Linq.OrderedEnumerable<int>
	// System.Linq.OrderedEnumerable<object,byte>
	// System.Linq.OrderedEnumerable<object,float>
	// System.Linq.OrderedEnumerable<object>
	// System.Nullable<int>
	// System.Predicate<DecomposeSystem.MaterialReward>
	// System.Predicate<GameUI.SettingPanel.ResolutionOption>
	// System.Predicate<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Predicate<System.ValueTuple<object,UnityEngine.Vector3,int>>
	// System.Predicate<System.ValueTuple<object,UnityEngine.Vector3>>
	// System.Predicate<System.ValueTuple<object,float,byte>>
	// System.Predicate<System.ValueTuple<object,float,int,int>>
	// System.Predicate<UnityEngine.Vector3>
	// System.Predicate<int>
	// System.Predicate<object>
	// System.ValueTuple<object,UnityEngine.Vector3,int>
	// System.ValueTuple<object,UnityEngine.Vector3>
	// System.ValueTuple<object,float,byte>
	// System.ValueTuple<object,float,int,int>
	// UnityEngine.Events.InvokableCall<byte>
	// UnityEngine.Events.InvokableCall<float>
	// UnityEngine.Events.InvokableCall<int>
	// UnityEngine.Events.UnityAction<byte>
	// UnityEngine.Events.UnityAction<float>
	// UnityEngine.Events.UnityAction<int>
	// UnityEngine.Events.UnityEvent<byte>
	// UnityEngine.Events.UnityEvent<float>
	// UnityEngine.Events.UnityEvent<int>
	// YokiFrame.CustomObjectFactory<object>
	// YokiFrame.EasyEvent.<GetListeners>d__9<object>
	// YokiFrame.EasyEvent<object>
	// YokiFrame.IObjectFactory<object>
	// YokiFrame.LinkUnRegister<object>
	// YokiFrame.PoolKit<object>
	// YokiFrame.PooledLinkedList.<Reverse>d__28<object>
	// YokiFrame.PooledLinkedList<object>
	// YokiFrame.Singleton<object>
	// YokiFrame.Unity.MonoSingleton<object>
	// }}

	public void RefMethods()
	{
		// object JsonMgr.Deserialize<object>(string,JsonType)
		// object JsonMgr.LoadConfig<object>(string,JsonType)
		// object JsonMgr.LoadData<object>(string,JsonType)
		// object LitJson.JsonMapper.ToObject<object>(string)
		// object System.Activator.CreateInstance<object>()
		// System.Collections.Generic.List<object> System.Collections.Generic.List<System.ValueTuple<object,UnityEngine.Vector3,int>>.ConvertAll<object>(System.Converter<System.ValueTuple<object,UnityEngine.Vector3,int>,object>)
		// System.Collections.Generic.List<object> System.Collections.Generic.List<object>.ConvertAll<object>(System.Converter<object,object>)
		// bool System.Linq.Enumerable.All<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// bool System.Linq.Enumerable.Any<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.AsEnumerable<object>(System.Collections.Generic.IEnumerable<object>)
		// bool System.Linq.Enumerable.Contains<object>(System.Collections.Generic.IEnumerable<object>,object)
		// bool System.Linq.Enumerable.Contains<object>(System.Collections.Generic.IEnumerable<object>,object,System.Collections.Generic.IEqualityComparer<object>)
		// int System.Linq.Enumerable.Count<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// object System.Linq.Enumerable.First<object>(System.Collections.Generic.IEnumerable<object>)
		// object System.Linq.Enumerable.First<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.KeyValuePair<object,float> System.Linq.Enumerable.FirstOrDefault<System.Collections.Generic.KeyValuePair<object,float>>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,float>>)
		// object System.Linq.Enumerable.FirstOrDefault<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Linq.IOrderedEnumerable<System.Collections.Generic.KeyValuePair<object,float>> System.Linq.Enumerable.OrderBy<System.Collections.Generic.KeyValuePair<object,float>,float>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,float>>,System.Func<System.Collections.Generic.KeyValuePair<object,float>,float>)
		// System.Linq.IOrderedEnumerable<object> System.Linq.Enumerable.OrderBy<object,float>(System.Collections.Generic.IEnumerable<object>,System.Func<object,float>)
		// System.Linq.IOrderedEnumerable<int> System.Linq.Enumerable.OrderByDescending<int,int>(System.Collections.Generic.IEnumerable<int>,System.Func<int,int>)
		// System.Linq.IOrderedEnumerable<object> System.Linq.Enumerable.OrderByDescending<object,byte>(System.Collections.Generic.IEnumerable<object>,System.Func<object,byte>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Reverse<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.ReverseIterator<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Select<object,int>(System.Collections.Generic.IEnumerable<object>,System.Func<object,int>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Select<System.Collections.Generic.KeyValuePair<object,int>,object>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>,System.Func<System.Collections.Generic.KeyValuePair<object,int>,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Select<object,object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Skip<object>(System.Collections.Generic.IEnumerable<object>,int)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.SkipIterator<object>(System.Collections.Generic.IEnumerable<object>,int)
		// System.Collections.Generic.IEnumerable<System.ValueTuple<object,float,int,int>> System.Linq.Enumerable.Take<System.ValueTuple<object,float,int,int>>(System.Collections.Generic.IEnumerable<System.ValueTuple<object,float,int,int>>,int)
		// System.Collections.Generic.IEnumerable<System.ValueTuple<object,float,int,int>> System.Linq.Enumerable.TakeIterator<System.ValueTuple<object,float,int,int>>(System.Collections.Generic.IEnumerable<System.ValueTuple<object,float,int,int>>,int)
		// System.Linq.IOrderedEnumerable<System.Collections.Generic.KeyValuePair<object,float>> System.Linq.Enumerable.ThenBy<System.Collections.Generic.KeyValuePair<object,float>,int>(System.Linq.IOrderedEnumerable<System.Collections.Generic.KeyValuePair<object,float>>,System.Func<System.Collections.Generic.KeyValuePair<object,float>,int>)
		// System.Linq.IOrderedEnumerable<object> System.Linq.Enumerable.ThenBy<object,int>(System.Linq.IOrderedEnumerable<object>,System.Func<object,int>)
		// System.Linq.IOrderedEnumerable<object> System.Linq.Enumerable.ThenBy<object,object>(System.Linq.IOrderedEnumerable<object>,System.Func<object,object>)
		// System.Linq.IOrderedEnumerable<object> System.Linq.Enumerable.ThenByDescending<object,int>(System.Linq.IOrderedEnumerable<object>,System.Func<object,int>)
		// System.Collections.Generic.List<System.ValueTuple<object,float,int,int>> System.Linq.Enumerable.ToList<System.ValueTuple<object,float,int,int>>(System.Collections.Generic.IEnumerable<System.ValueTuple<object,float,int,int>>)
		// System.Collections.Generic.List<int> System.Linq.Enumerable.ToList<int>(System.Collections.Generic.IEnumerable<int>)
		// System.Collections.Generic.List<object> System.Linq.Enumerable.ToList<object>(System.Collections.Generic.IEnumerable<object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Where<object>(System.Collections.Generic.IEnumerable<object>,System.Func<object,bool>)
		// System.Collections.Generic.IEnumerable<int> System.Linq.Enumerable.Iterator<object>.Select<int>(System.Func<object,int>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Iterator<System.Collections.Generic.KeyValuePair<object,int>>.Select<object>(System.Func<System.Collections.Generic.KeyValuePair<object,int>,object>)
		// System.Collections.Generic.IEnumerable<object> System.Linq.Enumerable.Iterator<object>.Select<object>(System.Func<object,object>)
		// System.Linq.IOrderedEnumerable<System.Collections.Generic.KeyValuePair<object,float>> System.Linq.IOrderedEnumerable<System.Collections.Generic.KeyValuePair<object,float>>.CreateOrderedEnumerable<int>(System.Func<System.Collections.Generic.KeyValuePair<object,float>,int>,System.Collections.Generic.IComparer<int>,bool)
		// System.Linq.IOrderedEnumerable<object> System.Linq.IOrderedEnumerable<object>.CreateOrderedEnumerable<int>(System.Func<object,int>,System.Collections.Generic.IComparer<int>,bool)
		// System.Linq.IOrderedEnumerable<object> System.Linq.IOrderedEnumerable<object>.CreateOrderedEnumerable<object>(System.Func<object,object>,System.Collections.Generic.IComparer<object>,bool)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,object>(System.Runtime.CompilerServices.TaskAwaiter&,object&)
		// System.Void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start<object>(object&)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// object UnityEngine.Component.GetComponent<object>()
		// object UnityEngine.Component.GetComponentInChildren<object>()
		// object UnityEngine.Component.GetComponentInChildren<object>(bool)
		// object[] UnityEngine.Component.GetComponentsInChildren<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>(bool)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>()
		// object UnityEngine.GameObject.GetComponentInChildren<object>(bool)
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
		// object UnityEngine.JsonUtility.FromJson<object>(string)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// object[] UnityEngine.Object.FindObjectsOfType<object>()
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform,bool)
		// object[] UnityEngine.Resources.ConvertObjects<object>(UnityEngine.Object[])
		// object[] UnityEngine.Resources.FindObjectsOfTypeAll<object>()
		// object UnityEngine.Resources.GetBuiltinResource<object>(string)
		// object UnityEngine.Resources.Load<object>(string)
		// object YokiFrame.EasyEvents.GetEvent<object>()
		// object YokiFrame.EasyEvents.GetOrAddEvent<object>()
		// YokiFrame.LinkUnRegister<object> YokiFrame.TypeEvent.Register<object>(System.Action<object>)
		// System.Void YokiFrame.TypeEvent.Send<object>(object)
		// System.Void YokiFrame.TypeEvent.UnRegister<object>(System.Action<object>)
		// System.Void YokiFrame.UIKit.ClosePanel<object>()
		// object YokiFrame.UIKit.OpenPanel<object>(YokiFrame.UILevel,YokiFrame.IUIData,string)
	}
}