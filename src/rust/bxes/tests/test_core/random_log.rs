use std::rc::Rc;

use bxes::{
    models::{
        BrafLifecycle, BxesEvent, BxesEventLog, BxesTraceVariant, BxesValue, Lifecycle,
        StandardLifecycle,
    },
    type_ids::{self, types_count},
};
use num_traits::FromPrimitive;
use rand::{distributions::Alphanumeric, rngs::ThreadRng, Rng};

pub fn generate_random_log() -> BxesEventLog {
    let mut rng = rand::thread_rng();
    BxesEventLog {
        version: rng.gen(),
        metadata: generate_random_attributes(&mut rng),
        variants: generate_random_variants(&mut rng),
    }
}

fn generate_random_variants(rng: &mut ThreadRng) -> Vec<BxesTraceVariant> {
    let variants_count = rng.gen_range(0..10);
    let mut variants = vec![];

    for _ in 0..variants_count {
        variants.push(generate_random_variant(rng));
    }

    variants
}

fn generate_random_variant(rng: &mut ThreadRng) -> BxesTraceVariant {
    let traces_count = rng.gen::<u32>();
    let mut events = vec![];

    let events_count = rng.gen_range(0..100);
    for _ in 0..events_count {
        events.push(generate_random_event(rng));
    }

    BxesTraceVariant {
        traces_count,
        events,
    }
}

fn generate_random_event(rng: &mut ThreadRng) -> BxesEvent {
    BxesEvent {
        name: generate_random_string_bxes_value(rng),
        timestamp: rng.gen(),
        lifecycle: generate_random_lifecycle(rng),
        attributes: generate_random_attributes(rng),
    }
}

fn generate_random_attributes(rng: &mut ThreadRng) -> Option<Vec<(BxesValue, BxesValue)>> {
    let attributes_count = rng.gen::<usize>();
    if attributes_count == 0 {
        None
    } else {
        let mut attributes = Vec::new();
        for _ in 0..attributes_count {
            attributes.push(generate_random_attribute(rng));
        }

        Some(attributes)
    }
}

fn generate_random_attribute(rng: &mut ThreadRng) -> (BxesValue, BxesValue) {
    (
        generate_random_string_bxes_value(rng),
        generate_random_bxes_value(rng),
    )
}

fn generate_random_string_bxes_value(rng: &mut ThreadRng) -> BxesValue {
    BxesValue::String(Rc::new(Box::new(generate_random_string(rng))))
}

fn generate_random_string(rng: &mut ThreadRng) -> String {
    let length = rng.gen_range(0..100);
    rng.sample_iter(&Alphanumeric)
        .take(length)
        .map(char::from)
        .collect()
}

fn generate_random_bxes_value(rng: &mut ThreadRng) -> BxesValue {
    match rng.gen_range(0..types_count()) as u8 {
        type_ids::I_32 => BxesValue::Int32(rng.gen()),
        type_ids::I_64 => BxesValue::Int64(rng.gen()),
        type_ids::U_32 => BxesValue::Uint32(rng.gen()),
        type_ids::U_64 => BxesValue::Uint64(rng.gen()),
        type_ids::F_32 => BxesValue::Float32(rng.gen()),
        type_ids::F_64 => BxesValue::Float64(rng.gen()),
        type_ids::BOOL => BxesValue::Bool(rng.gen()),
        type_ids::STRING => BxesValue::String(Rc::new(Box::new(generate_random_string(rng)))),
        type_ids::TIMESTAMP => BxesValue::Timestamp(rng.gen()),
        type_ids::BRAF_LIFECYCLE => BxesValue::BrafLifecycle(generate_random_braf_lifecycle()),
        type_ids::STANDARD_LIFECYCLE => {
            BxesValue::StandardLifecycle(generate_random_standard_lifecycle())
        }
        _ => panic!("Got unknown type id"),
    }
}

fn generate_random_lifecycle(rng: &mut ThreadRng) -> Lifecycle {
    match rng.gen_bool(0.5) {
        true => Lifecycle::Standard(generate_random_standard_lifecycle()),
        false => Lifecycle::Braf(generate_random_braf_lifecycle()),
    }
}

fn generate_random_braf_lifecycle() -> BrafLifecycle {
    generate_random_enum::<BrafLifecycle>(BrafLifecycle::VARIANT_COUNT)
}

fn generate_random_enum<T: FromPrimitive>(variant_count: usize) -> T {
    T::from_usize(variant_count).unwrap()
}

fn generate_random_standard_lifecycle() -> StandardLifecycle {
    generate_random_enum::<StandardLifecycle>(StandardLifecycle::VARIANT_COUNT)
}
