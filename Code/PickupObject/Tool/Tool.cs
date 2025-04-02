using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineAndDine
{
    internal partial class Tool : PickupObject
    {
        protected internal MaterialsList myContent;

        public virtual void Use()
        {

        }

        public MaterialsList GetContent()
        {
            //Not a property so that myContent can be sent as a ref into MaterialInteractions thingies
            return myContent;
        }
    }
}
