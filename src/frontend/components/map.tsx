import "leaflet/dist/leaflet.css";

import L from "leaflet";
import markerIcon2X from "leaflet/dist/images/marker-icon-2x.png";
import markerIcon from "leaflet/dist/images/marker-icon.png";
import markerShadow from "leaflet/dist/images/marker-shadow.png";
import { MapContainer, Marker, TileLayer } from "react-leaflet";

// @ts-ignore
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconUrl: markerIcon.src,
  iconRetinaUrl: markerIcon2X.src,
  shadowUrl: markerShadow.src,
});

type Props = {
  center?: L.LatLngExpression;
};

const POSITION: L.LatLngExpression = [51.505, -0.09];

const Map = ({ center }: Props) => {
  return (
    <MapContainer
      center={center || POSITION}
      zoom={center ? 4 : 2}
      scrollWheelZoom={false}
      className="h-[35vh] rounded-lg">
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      {center && <Marker position={center} />}
    </MapContainer>
  );
};

export default Map;
