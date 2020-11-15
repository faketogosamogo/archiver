using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FileArchiver.DataStructures
{
    /// <summary>
    /// Потокобразный Stack для BlockWithPosition
    /// Записываем и Считываем паралельно, после сигнала о прекращении записи коллекция вернёт оставшиеся блоки и будет возвращать null
    /// </summary>
    public class ConcurrencyBlockStack
    {
        private  object _blocksLocker;
        private Stack<BlockWithPosition> _blocks;

        private bool _isStopWritingBlocks;

        public ConcurrencyBlockStack()
        {
            _blocksLocker = new object();
            _blocks = new Stack<BlockWithPosition>();
        }

        /// <summary>
        /// Добавить элемент в коллекцию
        /// </summary>
        /// <param name="block"></param>
        public void Push(BlockWithPosition block)
        {
            lock (_blocksLocker)//enter
            {
                _blocks.Push(block);
                Monitor.Pulse(_blocksLocker);//сообщаем
            }//exit
        }
        /// <summary>
        /// Вытащить элемент из коллекции
        /// </summary>
        /// <returns>Полученный блок, в случае прекращения записи возвращает null</returns>
        public BlockWithPosition Pop()
        {
            lock (_blocksLocker)
            {
                BlockWithPosition block = null;
                while (!_isStopWritingBlocks && _blocks.Count==0)
                {
                    Monitor.Wait(_blocksLocker);
                }
                while (true)
                {
                    if (_blocks.Count == 0) return null;
                    if (_blocks.TryPop(out block))break;
                }

                return block;
            }
        }

        /// <summary>
        /// Сообщить, что запись в поток окончена.
        /// При повторном вызове метода ничего не произойдёт
        /// </summary>
        public void StopWriting()
        {
            lock (_blocksLocker)//Не уверен, что нужен отдельный locker, т.к обращаемся и из другой функции
            {
                _isStopWritingBlocks = true;
            }
        }
        /// <summary>
        /// Получение количества блоков
        /// </summary>
        /// <returns>Количество блоков</returns>
        public int Count()
        {
            lock (_blocksLocker)
            {
                int count = _blocks.Count;
                return count;
            }
        }
    }
}
