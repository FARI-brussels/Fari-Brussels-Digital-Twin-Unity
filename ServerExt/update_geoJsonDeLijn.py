import csv
import json
from collections import defaultdict
import requests
import zipfile
import io
import os

def convert_gtfs_to_geojson_shape(csv_reader):
    """Convertit le fichier shapes.txt en GeoJSON"""
    shapes = defaultdict(list)
    
    # Lire le contenu du CSV
    for row in csv_reader:
        shape_id = row['shape_id']
        lat = float(row['shape_pt_lat'])
        lon = float(row['shape_pt_lon'])
        sequence = int(row['shape_pt_sequence'])
        dist_traveled = float(row['shape_dist_traveled']) if 'shape_dist_traveled' in row and row['shape_dist_traveled'] else 0.0
        
        shapes[shape_id].append({
            'sequence': sequence,
            'lat': lat,
            'lon': lon,
            'dist_traveled': dist_traveled
        })
    
    # Construire le GeoJSON
    geojson = {
        'type': 'FeatureCollection',
        'features': []
    }
    
    for shape_id, points in shapes.items():
        points.sort(key=lambda x: x['sequence'])  # Trier par séquence
        coordinates = [[point['lon'], point['lat']] for point in points]
        
        feature = {
            'type': 'Feature',
            'properties': {
                'shape_id': shape_id,
                'point_count': len(points),
                'total_distance': points[-1]['dist_traveled'] if points else 0
            },
            'geometry': {
                'type': 'LineString',
                'coordinates': coordinates
            }
        }
        geojson['features'].append(feature)
    
    return geojson

def convert_gtfs_to_json_routes(csv_reader):
    """Convertit le fichier routes.txt en JSON"""
    routes_data = {}
    
    for row in csv_reader:
        route_id = row["route_id"]
        route = {
            "agency_id": row["agency_id"],
            "route_short_name": row["route_short_name"],
            "route_long_name": row["route_long_name"],
            "route_desc": row["route_desc"],
            "route_type": row["route_type"],
            "route_url": row["route_url"],
            "route_color": row["route_color"],
            "route_text_color": row["route_text_color"]
        }
        routes_data[route_id] = route
    
    return routes_data

def convert_gtfs_to_json_trip(csv_reader):
    trips_data = {} 
    for row in csv_reader:
        trip_id = row["trip_id"]
        trip = {
            "trip_info": {
                "route_id": row["route_id"],
                "service_id": row["service_id"],
                "trip_headsign": row["trip_headsign"],
                "trip_short_name": row["trip_short_name"],
                "direction_id": row["direction_id"],
                "block_id": row["block_id"],
                "shape_id": row["shape_id"]
            }
        }
        trips_data[trip_id] = trip  
    return trips_data



def convert_gtfs_to_geojson_stop(csv_reader):
    """Convertit le fichier stops.txt en GeoJSON"""
    geojson = {
        'type': 'FeatureCollection',
        'features': []
    }
    
    for row in csv_reader:
        stop_id = row['stop_id']
        stop_code = row['stop_code']
        stop_name = row['stop_name']
        lat = float(row['stop_lat'])
        lon = float(row['stop_lon'])
        stop_url = row['stop_url']
        wheelchair_boarding = int(row['wheelchair_boarding'])
        
        feature = {
            'type': 'Feature',
            'properties': {
                'stop_id': stop_id,
                'stop_code': stop_code,
                'stop_name': stop_name,
                'stop_url': stop_url,
                'wheelchair_accessible': wheelchair_boarding == 1
            },
            'geometry': {
                'type': 'Point',
                'coordinates': [lon, lat]
            }
        }
        geojson['features'].append(feature)
    
    return geojson

# Exemple d'utilisation
if __name__ == "__main__":
    BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    ASSETS_PATH = os.path.join(BASE_DIR, "geoJson")
    os.makedirs(ASSETS_PATH, exist_ok=True)

    url = "https://api.delijn.be/gtfs/static/v3/gtfs_transit.zip"
    headers = {
        "Ocp-Apim-Subscription-Key": "a2ad3f2dbdd047dcbd07b9f6f9f14c3c"
    }
    response = requests.get(url, headers=headers) 
    zip_file_bytes = io.BytesIO(response.content)
    
    with zipfile.ZipFile(zip_file_bytes, "r") as z:
        if "shapes.txt" in z.namelist():
            with z.open("shapes.txt") as csvfile:
                shapes_geojson = convert_gtfs_to_geojson_shape(csv.DictReader(io.TextIOWrapper(csvfile, "utf-8")))

        if "stops.txt" in z.namelist():
            with z.open("stops.txt") as csvfile:
                stops_geojson = convert_gtfs_to_geojson_stop(csv.DictReader(io.TextIOWrapper(csvfile, "utf-8")))
        
        if "trips.txt" in z.namelist():
            with z.open("trips.txt") as csvfile:
                trips_data = convert_gtfs_to_json_trip(csv.DictReader(io.TextIOWrapper(csvfile, "utf-8")))
        if "routes.txt" in z.namelist():
            with z.open("routes.txt") as csvfile:
                routes_data = convert_gtfs_to_json_routes(csv.DictReader(io.TextIOWrapper(csvfile, "utf-8")))

    with open(os.path.join(ASSETS_PATH, "shapes.json"), "w", encoding="utf-8") as f:
        json.dump(shapes_geojson, f, indent=2, ensure_ascii=False)

    with open(os.path.join(ASSETS_PATH, "stops.json"), "w", encoding="utf-8") as f:
        json.dump(stops_geojson, f, indent=2, ensure_ascii=False)
        
    with open(os.path.join(ASSETS_PATH, "trips.json"), "w", encoding="utf-8") as f:
        json.dump(trips_data, f, indent=2, ensure_ascii=False)
    
    with open(os.path.join(ASSETS_PATH, "routes.json"), "w", encoding="utf-8") as f:
        json.dump(routes_data, f, indent=2, ensure_ascii=False)
                    
    
    """
    input_file_shape = "shapes.txt"
    output_file_shape = "shapes.json"
    input_file_stop = "stops.txt"
    output_file_stop = "stops_DL.json"
    
    num_stops = convert_gtfs_to_geojson_stop(input_file_stop, output_file_stop)
    num_shapes = convert_gtfs_to_geojson_shape(input_file_shape, output_file_shape)
    
    print(f"Conversion réussie ! {num_shapes} trajets ont été convertis au format GeoJSON.")
    print(f"Conversion réussie ! {num_stops} trajets ont été convertis au format GeoJSON.")"""
    