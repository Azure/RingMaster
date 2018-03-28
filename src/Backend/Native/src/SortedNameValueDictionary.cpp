#include <cliext/adapter>
#include <cliext/map>

#pragma comment(lib, "mscoree.lib")

using namespace cliext;
using namespace System;
using namespace System::Threading;
using namespace System::Collections::Generic;

namespace Microsoft {
namespace Azure {
namespace Networking {
namespace Infrastructure {
namespace RingMaster {
namespace Backend {
namespace Native {

    // SortedNameValueDictionary implements a mapping from String -> ValueType
    // The names are kept in sorted order
    generic <class ValueType> 
    where ValueType : ref class
    public ref class SortedNameValueDictionary : IDictionary<String^, ValueType>
    {
    private:
        typedef map<String^, Object^> MapType;

        IDictionary<String^, Object^>^ underlyingDictionary;
        System::Threading::SpinLock dictionaryLock;

    public:

        SortedNameValueDictionary()
            : SortedNameValueDictionary(nullptr)
        {
        }

        SortedNameValueDictionary(IEnumerable<KeyValuePair<String^, ValueType>>^ elements)
        {
            underlyingDictionary = gcnew MapType(gcnew MapType::key_compare(&KeyCompare));

            if (elements != nullptr)
            {
                auto enumerator = elements->GetEnumerator();
                while (enumerator->MoveNext())
                {
                    underlyingDictionary->Add(enumerator->Current.Key, enumerator->Current.Value);
                }
            }
        }

        virtual IEnumerable<String^>^ GetKeysGreaterThan(String^ key)
        {
            MapType^ underlyingMap = (MapType^)underlyingDictionary;
            MapType::iterator iterBegin = underlyingMap->begin();
            MapType::iterator iterEnd = underlyingMap->end();

            if (!String::IsNullOrEmpty(key))
            {
                // Position the iterator at the first key in the map that is greater than
                // the given key.
                iterBegin = underlyingMap->upper_bound(key);
            }

            return gcnew KeyEnumerable(iterBegin, iterEnd);
        }

        virtual System::Collections::IEnumerator^ GetNonGenericEnumerator() = System::Collections::IEnumerable::GetEnumerator
        {
            return GetEnumerator();
        }

        virtual IEnumerator<KeyValuePair<String^, ValueType>>^ GetEnumerator()
        {
            return gcnew KeyValueEnumerator(underlyingDictionary->GetEnumerator());
        }

        // Inherited via IDictionary
        virtual property int Count
        {
            int get() 
            { 
                return underlyingDictionary->Count; 
            }
        }

        virtual property bool IsReadOnly
        {
            bool get() 
            { 
                return underlyingDictionary->IsReadOnly; 
            }
        }

        virtual property ICollection<String ^>^ Keys
        {
            ICollection<String^>^ get() 
            {
                return underlyingDictionary->Keys;
            }
        }

        virtual property ICollection<ValueType>^ Values
        {
            ICollection<ValueType>^ get() 
            { 
                return gcnew ValueCollection(underlyingDictionary->Values); 
            }
        }

        virtual property ValueType default[String^]
        {
            ValueType get(String^ key) 
            { 
                return (ValueType)underlyingDictionary->default[key]; 
            }

            void set(String^ key, ValueType val) 
            { 
                underlyingDictionary->default[key] = val; 
            }
        }

        virtual void Add(KeyValuePair<String^, ValueType> item)
        {
            underlyingDictionary->Add(KeyValuePair<String^, Object^>(item.Key, item.Value));
        }

        virtual void Clear()
        {
            underlyingDictionary->Clear();
        }

        virtual bool Contains(KeyValuePair<String^, ValueType> item)
        {
            return underlyingDictionary->Contains(KeyValuePair<String^, Object^>(item.Key, item.Value));
        }

        virtual void CopyTo(array<KeyValuePair<String^, ValueType>, 1>^ targetArray, int arrayIndex)
        {
            if (targetArray == nullptr)
            {
                throw gcnew ArgumentNullException("targetArray");
            }

            if ((arrayIndex < 0) || (arrayIndex >= targetArray->Length))
            {
                throw gcnew ArgumentOutOfRangeException("arrayIndex");
            }

            if (underlyingDictionary->Count >(targetArray->Length - arrayIndex))
            {
                throw gcnew ArgumentException("targetArray is too small");
            }

            auto enumerator = underlyingDictionary->GetEnumerator();
            while (enumerator->MoveNext())
            {
                targetArray[arrayIndex++] = KeyValuePair<String^, ValueType>(enumerator->Current.Key, (ValueType)enumerator->Current.Value);
            }
        }

        virtual bool Remove(String^ key)
        {
            return underlyingDictionary->Remove(key);
        }

        virtual bool Remove(KeyValuePair<String^, ValueType> item)
        {
            return underlyingDictionary->Remove(KeyValuePair<String^, Object^>(item.Key, item.Value));
        }

        virtual bool ContainsKey(String ^key)
        {
            return underlyingDictionary->ContainsKey(key);
        }

        virtual void Add(String^ key, ValueType value)
        {
            underlyingDictionary->Add(key, value);
        }

        virtual bool TryGetValue(String^ key, ValueType %value)
        {
            Object^ objectValue;
            if (underlyingDictionary->TryGetValue(key, objectValue))
            {
                value = (ValueType)objectValue;
                return true;
            }

            return false;
        }

        private:

        static bool KeyCompare(String^ first, String^ second)
        {
            return String::Compare(first, second, StringComparison::Ordinal) < 0;
        }

        // KeyValueEnumerator implements an IEnumerator<KeyValuePair<String^, ValueType>>
        // by wrapping an underlying an IEnumerator<KeyValuePair<String^, Object^>>
        ref class KeyValueEnumerator : IEnumerator<KeyValuePair<String^, ValueType>>
        {
        private:

            IEnumerator<KeyValuePair<String^, Object^>>^ underlyingEnumerator;

        public:

            KeyValueEnumerator(IEnumerator<KeyValuePair<String^, Object^>>^ enumerator)
            {
                underlyingEnumerator = enumerator;
            }

            virtual ~KeyValueEnumerator()
            {
                delete underlyingEnumerator;
            }

            virtual property Object^ CurrentObject
            {
                Object^ get() = System::Collections::IEnumerator::Current::get
                {
                    return underlyingEnumerator->Current;
                }
            }

            virtual property KeyValuePair<String^, ValueType> Current
            {
                KeyValuePair<String^, ValueType> get ()
                {
                    return KeyValuePair<String^, ValueType>(underlyingEnumerator->Current.Key, (ValueType)underlyingEnumerator->Current.Value); 
                }
            }

            virtual bool MoveNext()
            {
                return underlyingEnumerator->MoveNext();
            }

            virtual void Reset()
            {
                return underlyingEnumerator->Reset();
            }
        };

        // KeyEnumerator implements an IEnumerator<String^>
        // by wrapping an underlying an IEnumerator<KeyValuePair<String^, Object^>>
        ref class KeyEnumerator : IEnumerator<String^>
        {
        private:

            IEnumerator<KeyValuePair<String^, Object^>>^ underlyingEnumerator;

        public:

            KeyEnumerator(IEnumerator<KeyValuePair<String^, Object^>>^ enumerator)
            {
                underlyingEnumerator = enumerator;
            }

            virtual ~KeyEnumerator()
            {
                delete underlyingEnumerator;
            }

            virtual property Object^ CurrentObject
            {
                Object^ get() = System::Collections::IEnumerator::Current::get
                {
                    return underlyingEnumerator->Current;
                }
            }

            virtual property String^ Current
            {
                String^ get()
                {
                    return underlyingEnumerator->Current.Key;
                }
            }

            virtual bool MoveNext()
            {
                return underlyingEnumerator->MoveNext();
            }

            virtual void Reset()
            {
                return underlyingEnumerator->Reset();
            }
        };

        // KeyEnumerable implements IEnumerable<String^> by
        // wrapping an underlying IEnumerable<KeyValuePair<String^, Object^>>
        ref class KeyEnumerable : IEnumerable<String^>
        {
        private:

            MapType::iterator beginIterator;
            MapType::iterator endIterator;

        public:

            KeyEnumerable(MapType::iterator begin, MapType::iterator end)
            {
                beginIterator = begin;
                endIterator = end;
            }

            virtual System::Collections::IEnumerator^ GetNonGenericEnumerator() = System::Collections::IEnumerable::GetEnumerator
            {
                return GetEnumerator();
            }

            // Inherited via ICollection
            virtual System::Collections::Generic::IEnumerator<System::String ^> ^ GetEnumerator()
            {
                return gcnew Enumerator(beginIterator, endIterator);
            }

        private:

            ref class Enumerator : IEnumerator<String^>
            {
            private:
                MapType::iterator beginIterator;
                MapType::iterator iterator;
                MapType::iterator endIterator;

            public:

                Enumerator(MapType::iterator begin, MapType::iterator end)
                {
                    beginIterator = begin;
                    iterator = end;
                    endIterator = end;
                }

                virtual~Enumerator()
                {
                }

                virtual property Object^ CurrentObject
                {
                    Object^ get() = System::Collections::IEnumerator::Current::get
                    {
                        return Current;
                    }
                }

                virtual property String^ Current
                {
                    String^ get() { return iterator->first; }
                }

                virtual bool MoveNext()
                {
                    if (iterator == endIterator)
                    {
                        iterator = beginIterator;
                    }
                    else
                    {
                        iterator++;
                    }

                    return iterator != endIterator;
                }

                virtual void Reset()
                {
                    iterator = endIterator;
                }
            };
        };

        // ValueEnumerator implements an IEnumerator<ValueType> by wrapping
        // an underlying IEnumerator<Object^>
        ref class ValueEnumerator : IEnumerator<ValueType>
        {
        private:

            IEnumerator<Object^>^ underlyingEnumerator;

        public:

            ValueEnumerator(IEnumerator<Object^>^ enumerator)
            {
                underlyingEnumerator = enumerator;
            }

            virtual ~ValueEnumerator()
            {
                delete underlyingEnumerator;
            }

            virtual property Object^ CurrentObject
            {
                Object^ get() = System::Collections::IEnumerator::Current::get
                {
                    return underlyingEnumerator->Current;
                }
            }

            virtual property ValueType Current
            {
                ValueType get()
                {
                    return (ValueType)underlyingEnumerator->Current;
                }
            }

            virtual bool MoveNext()
            {
                return underlyingEnumerator->MoveNext();
            }

            virtual void Reset()
            {
                return underlyingEnumerator->Reset();
            }
        };

        // ValueCollection implements ICollection<ValueType> by wrapping an
        // underlying ICollection<Object^>
        ref class ValueCollection : ICollection<ValueType>
        {
        private:
            ICollection<Object^>^ underlyingCollection;

        public:
            ValueCollection(ICollection<Object^>^ collection)
            {
                underlyingCollection = collection;
            }

            virtual System::Collections::IEnumerator^ GetNonGenericEnumerator() = System::Collections::IEnumerable::GetEnumerator
            {
                return GetEnumerator();
            }

            // Inherited via ICollection
            virtual System::Collections::Generic::IEnumerator<ValueType> ^ GetEnumerator()
            {
                return gcnew ValueEnumerator(underlyingCollection->GetEnumerator());
            }

            virtual property int Count
            {
                int get() { return underlyingCollection->Count; }
            }

            virtual property bool IsReadOnly
            {
                bool get() { return underlyingCollection->IsReadOnly; }
            }

            virtual void Add(ValueType item)
            {
                underlyingCollection->Add(item);
            }

            virtual void Clear()
            {
                underlyingCollection->Clear();
            }

            virtual bool Contains(ValueType item)
            {
                return underlyingCollection->Contains(item);
            }

            virtual void CopyTo(array<ValueType, 1>^ targetArray, int arrayIndex)
            {
                if (targetArray == nullptr)
                {
                    throw gcnew ArgumentNullException("targetArray");
                }

                if ((arrayIndex < 0) || (arrayIndex >= targetArray->Length))
                {
                    throw gcnew ArgumentOutOfRangeException("arrayIndex");
                }

                if (targetArray->Rank != 1)
                {
                    throw gcnew ArgumentException("targetArray is multidimensional");
                }

                if (underlyingCollection->Count >(targetArray->Length - arrayIndex))
                {
                    throw gcnew ArgumentException("targetArray is too small");
                }

                auto enumerator = underlyingCollection->GetEnumerator();
                while (enumerator->MoveNext())
                {
                    targetArray[arrayIndex++] = (ValueType)enumerator->Current;
                }
            }

            virtual bool Remove(ValueType item)
            {
                return underlyingCollection->Remove(item);
            }
        };
    };

}}}}}}}