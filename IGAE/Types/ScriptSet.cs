namespace STTUMM.IGAE.Types;

public class ScriptSet : igObject
{

    public ScriptSetList _list; // 18
    
    public ScriptSet(igObject basic)
    {
        _container = basic._container;
        offset = basic.offset;
        name = basic.name;
        itemCount = basic.itemCount;
        length = basic.length;
        data = basic.data;
        fields = basic.fields;
        children = basic.children;
    }
    
    public class ScriptSetList : igObject
    {
        private uint _properties; //1C igUnsignedIntMetaField
        private uint _nextHistory; //20 igUnsignedIntMetaField
        private object _chooseNextRandomly; //1C igBitFieldMetaField
        private object _allowRandomRepeats; // 1C igBitFieldMetaField
        private object _randomChoiceCount; // 0 igBitFieldMetaField
        
    
        public ScriptSetList(igObject basic)
        {
            _container = basic._container;
            offset = basic.offset;
            name = basic.name;
            itemCount = basic.itemCount;
            length = basic.length;
            data = basic.data;
            fields = basic.fields;
            children = basic.children;
        }
    }
}